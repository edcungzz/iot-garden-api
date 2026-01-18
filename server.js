const mqtt = require('mqtt');
const { Client } = require('pg');
const express = require('express');
const cors = require('cors');

// --- Configuration ---
const MQTT_BROKER = 'mqtts://eu1.cloud.thethings.network'; // Change region if needed (e.g., nam1, au1)
const MQTT_APP_ID = 'your-app-id'; // REPLACE THIS
const MQTT_ACCESS_KEY = 'your-access-key'; // REPLACE THIS (starts with NNSXS...)
const TOPIC = 'v3/+/devices/+/up'; // Standard TTN uplink topic

const DB_CONNECTION_STRING = "postgresql://postgres.fjljkiwiobhbazzqusie:osWDtHrdSWQ96Z29@aws-1-ap-southeast-1.pooler.supabase.com:6543/postgres";
const PORT = 3001;

// --- Database Setup ---
const db = new Client({
    connectionString: DB_CONNECTION_STRING,
    ssl: { rejectUnauthorized: false }
});

db.connect().then(() => console.log('✅ Connected to PostgreSQL')).catch(err => console.error('❌ DB Connection Error:', err));

// --- Express Server ---
const app = express();
app.use(cors());
app.use(express.json());

// API: Get Latest Reading
app.get('/api/sensor/latest', async (req, res) => {
    try {
        // Fetch from SensorRealtime (Current Status) or SensorLogs (History)
        // Using SensorRealtime for "Latest"
        const result = await db.query('SELECT * FROM "SensorRealtime" ORDER BY "LastUpdated" DESC LIMIT 1');
        if (result.rows.length > 0) {
            res.json(result.rows[0]);
        } else {
            res.status(404).json({ message: 'No data found' });
        }
    } catch (err) {
        console.error(err);
        res.status(500).json({ error: 'Database error' });
    }
});

// --- MQTT Client ---
const mqttClient = mqtt.connect(MQTT_BROKER, {
    username: MQTT_APP_ID,
    password: MQTT_ACCESS_KEY
});

mqttClient.on('connect', () => {
    console.log('🔗 Connected to MQTT Broker');
    mqttClient.subscribe(TOPIC, (err) => {
        if (!err) {
            console.log(`📡 Subscribed to ${TOPIC}`);
        } else {
            console.error('❌ Subscription error:', err);
        }
    });
});

mqttClient.on('message', async (topic, message) => {
    console.log(`📩 Message received on ${topic}`);
    try {
        const payload = JSON.parse(message.toString());

        // TTN payload usually has 'uplink_message.frm_payload' which is Base64 encoded.
        // Or if you are using the JS decoder in TTN Console, it might be in 'uplink_message.decoded_payload'.
        // The user spec implies we are decoding the RAW bytes here.
        // Assuming the device sends raw bytes and we get them in frm_payload (Base64).

        if (payload.uplink_message && payload.uplink_message.frm_payload) {
            const buffer = Buffer.from(payload.uplink_message.frm_payload, 'base64');

            if (buffer.length !== 14) {
                console.warn(`⚠ Unexpected payload length: ${buffer.length} bytes (Expected 14)`);
                // Proceeding carefully or skipping? 
                // Let's log it but try to parse if possible or skip.
                if (buffer.length < 14) return;
            }

            // Decode Data (Big Endian)
            const humidity = buffer.readInt16BE(0) / 10.0;
            const temperature = buffer.readInt16BE(2) / 10.0;
            const ec = buffer.readUInt16BE(4); // us/cm
            const ph = buffer.readInt16BE(6) / 10.0;
            const n = buffer.readUInt16BE(8);  // mg/kg
            const p = buffer.readUInt16BE(10); // mg/kg
            const k = buffer.readUInt16BE(12); // mg/kg

            const deviceId = payload.end_device_ids ? payload.end_device_ids.device_id : 'unknown_device';

            console.log(`📊 Decoded: Temp=${temperature}°C, Hum=${humidity}%, EC=${ec}, pH=${ph}, NPK=${n}-${p}-${k}`);

            // 1. Upsert into SensorRealtime (For Dashboard Status)
            const upsertQuery = `
                INSERT INTO "SensorRealtime" (
                    "DeviceId", "Temperature", "Humidity", "Ph", "N", "P", "K", "SoilMoisture", "Rssi", "Snr", "LastUpdated"
                )
                VALUES ($1, $2, $3, $4, $5, $6, $7, $8, 0, 0, $9)
                ON CONFLICT ("DeviceId")
                DO UPDATE SET 
                    "Temperature" = EXCLUDED."Temperature",
                    "Humidity" = EXCLUDED."Humidity",
                    "Ph" = EXCLUDED."Ph",
                    "N" = EXCLUDED."N",
                    "P" = EXCLUDED."P",
                    "K" = EXCLUDED."K",
                    "SoilMoisture" = EXCLUDED."SoilMoisture",
                    "LastUpdated" = EXCLUDED."LastUpdated";
            `;
            const now = new Date().toISOString();
            // Note: Mapping Humidity to SoilMoisture as per existing system logic
            await db.query(upsertQuery, [deviceId, temperature, humidity, ph, n, p, k, humidity, now]);

            // 2. Insert into SensorLogs (For History)
            const logQuery = `
                INSERT INTO "SensorLogs" (
                    "DeviceId", "Timestamp", "Temperature", "Humidity", "Ph", "N", "P", "K", "SoilMoisture", "Pressure", "AvgSoilMoisture"
                )
                VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, 0, $10);
            `;
            await db.query(logQuery, [deviceId, now, temperature, humidity, ph, n, p, k, humidity, humidity]);

            console.log('✅ Data saved to SensorRealtime & SensorLogs (Compatible with Dashboard)');
        }
    } catch (err) {
        console.error('❌ Error processing message:', err);
    }
});

app.listen(PORT, () => {
    console.log(`🚀 Server running on http://localhost:${PORT}`);
});
