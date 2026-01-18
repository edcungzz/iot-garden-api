const { Client } = require('pg');

const connectionString = "postgresql://postgres.fjljkiwiobhbazzqusie:osWDtHrdSWQ96Z29@aws-1-ap-southeast-1.pooler.supabase.com:6543/postgres";

async function injectTestData() {
    const client = new Client({
        connectionString: connectionString,
        ssl: { rejectUnauthorized: false }
    });

    try {
        await client.connect();

        const now = new Date();
        const deviceId = 'test-device-01';

        // Upsert test data
        // Note: I'm updating "LastUpdated" to NOW
        const query = `
            INSERT INTO "SensorRealtime" (
                "DeviceId", "Temperature", "Humidity", "Ph", "N", "P", "K", "SoilMoisture", "Rssi", "Snr", "LastUpdated"
            )
            VALUES ($1, 25.5, 60.0, 6.5, 100, 50, 200, 60.0, -50, 9.5, $2)
            ON CONFLICT ("DeviceId")
            DO UPDATE SET 
                "Temperature" = EXCLUDED."Temperature",
                "Humidity" = EXCLUDED."Humidity",
                "LastUpdated" = EXCLUDED."LastUpdated";
        `;

        await client.query(query, [deviceId, now]);
        console.log(`✅ Injecting test data for ${deviceId} at ${now.toISOString()}`);

    } catch (err) {
        console.error('❌ Error injecting data:', err);
    } finally {
        await client.end();
    }
}

injectTestData();
