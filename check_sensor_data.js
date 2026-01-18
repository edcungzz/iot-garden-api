const { Client } = require('pg');

const connectionString = "postgresql://postgres.fjljkiwiobhbazzqusie:osWDtHrdSWQ96Z29@aws-1-ap-southeast-1.pooler.supabase.com:6543/postgres";

async function checkData() {
    const client = new Client({
        connectionString: connectionString,
        ssl: { rejectUnauthorized: false }
    });

    try {
        await client.connect();
        console.log('🔌 Connected to database\n');

        // Check Sensors table
        console.log('=== SENSORS TABLE ===');
        const sensors = await client.query('SELECT * FROM "Sensors" ORDER BY "LastSeen" DESC LIMIT 10');
        console.log(`Found ${sensors.rows.length} sensors:`);
        sensors.rows.forEach(s => {
            console.log(`  - ${s.Id}: ${s.Name} = ${s.Value} ${s.Unit} (${s.Status}) - Last seen: ${s.LastSeen}`);
        });

        // Check SensorLogs table
        console.log('\n=== SENSOR LOGS TABLE ===');
        const logs = await client.query('SELECT * FROM "SensorLogs" ORDER BY "Timestamp" DESC LIMIT 20');
        console.log(`Found ${logs.rows.length} log entries:`);
        logs.rows.forEach(l => {
            console.log(`  - ${l.SensorId}: ${l.Value} at ${l.Timestamp}`);
        });

        // Check sensor_logs table (Supabase)
        console.log('\n=== SUPABASE SENSOR_LOGS TABLE ===');
        try {
            const supabaseLogs = await client.query('SELECT * FROM sensor_logs ORDER BY created_at DESC LIMIT 10');
            console.log(`Found ${supabaseLogs.rows.length} Supabase log entries:`);
            supabaseLogs.rows.forEach(l => {
                console.log(`  - Temp: ${l.temp}°C, Humi: ${l.humi}%, EC: ${l.ec}, pH: ${l.ph} at ${l.created_at}`);
            });
        } catch (err) {
            console.log('  (Table does not exist or is empty)');
        }

    } catch (err) {
        console.error('❌ Error:', err.message);
    } finally {
        await client.end();
    }
}

checkData();
