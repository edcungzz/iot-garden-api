const { Client } = require('pg');

const connectionString = "postgresql://postgres.fjljkiwiobhbazzqusie:osWDtHrdSWQ96Z29@aws-1-ap-southeast-1.pooler.supabase.com:6543/postgres";

async function createAllSensors() {
    const client = new Client({
        connectionString: connectionString,
        ssl: { rejectUnauthorized: false }
    });

    try {
        await client.connect();
        console.log('🔌 Connected to database\n');

        const deviceId = 'heltec-v3-01';
        const now = new Date().toISOString();

        const sensors = [
            { type: 'temp', name: 'heltec-v3-01 TEMP', unit: '°C', value: 26.6 },
            { type: 'humi', name: 'heltec-v3-01 HUMI', unit: '%', value: 91.8 },
            { type: 'ec', name: 'heltec-v3-01 EC', unit: 'µS/cm', value: 0 },
            { type: 'ph', name: 'heltec-v3-01 PH', unit: 'pH', value: 4.5 },
            { type: 'n', name: 'heltec-v3-01 N', unit: 'mg/kg', value: 0 },
            { type: 'p', name: 'heltec-v3-01 P', unit: 'mg/kg', value: 6 },
            { type: 'k', name: 'heltec-v3-01 K', unit: 'mg/kg', value: 0 }
        ];

        console.log('Creating/Updating sensors...\n');

        for (const sensor of sensors) {
            const sensorId = `${deviceId}_${sensor.type}`;

            // Check if sensor exists
            const checkResult = await client.query(
                'SELECT * FROM "Sensors" WHERE "Id" = $1',
                [sensorId]
            );

            if (checkResult.rows.length > 0) {
                // Update existing sensor
                await client.query(
                    `UPDATE "Sensors" 
                     SET "Name" = $1, "Type" = $2, "Value" = $3, "Unit" = $4, "Status" = $5, "LastSeen" = $6
                     WHERE "Id" = $7`,
                    [sensor.name, sensor.type, sensor.value, sensor.unit, 'online', now, sensorId]
                );
                console.log(`✅ Updated: ${sensorId} = ${sensor.value} ${sensor.unit}`);
            } else {
                // Insert new sensor
                await client.query(
                    `INSERT INTO "Sensors" ("Id", "Name", "Type", "Value", "Unit", "Status", "LastSeen")
                     VALUES ($1, $2, $3, $4, $5, $6, $7)`,
                    [sensorId, sensor.name, sensor.type, sensor.value, sensor.unit, 'online', now]
                );
                console.log(`✨ Created: ${sensorId} = ${sensor.value} ${sensor.unit}`);
            }
        }

        console.log('\n✅ All sensors created/updated successfully!');
        console.log('\nCurrent sensors in database:');

        const allSensors = await client.query('SELECT * FROM "Sensors" ORDER BY "Type"');
        allSensors.rows.forEach(s => {
            console.log(`  - ${s.Id}: ${s.Name} = ${s.Value} ${s.Unit} (${s.Status})`);
        });

    } catch (err) {
        console.error('❌ Error:', err.message);
    } finally {
        await client.end();
    }
}

createAllSensors();
