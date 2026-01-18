const { Client } = require('pg');

const connectionString = "postgresql://postgres.fjljkiwiobhbazzqusie:osWDtHrdSWQ96Z29@aws-1-ap-southeast-1.pooler.supabase.com:6543/postgres";

async function clearAllHistoryData() {
    const client = new Client({
        connectionString: connectionString,
        ssl: { rejectUnauthorized: false }
    });

    try {
        await client.connect();
        console.log('🔌 Connected to database\n');

        // Check current data
        console.log('📊 Checking current data...\n');

        const checkLogs = await client.query('SELECT COUNT(*) FROM "SensorLogs"');
        console.log(`SensorLogs: ${checkLogs.rows[0].count} records`);

        const checkSupabase = await client.query('SELECT COUNT(*) FROM sensor_logs');
        console.log(`sensor_logs (Supabase): ${checkSupabase.rows[0].count} records\n`);

        // Delete all data
        console.log('🗑️  Deleting all history data...\n');

        const deleteLogs = await client.query('DELETE FROM "SensorLogs"');
        console.log(`✅ Deleted ${deleteLogs.rowCount} records from SensorLogs`);

        const deleteSupabase = await client.query('DELETE FROM sensor_logs');
        console.log(`✅ Deleted ${deleteSupabase.rowCount} records from sensor_logs\n`);

        // Verify
        const verifyLogs = await client.query('SELECT COUNT(*) FROM "SensorLogs"');
        const verifySupabase = await client.query('SELECT COUNT(*) FROM sensor_logs');

        console.log('✅ Verification:');
        console.log(`SensorLogs: ${verifyLogs.rows[0].count} records remaining`);
        console.log(`sensor_logs: ${verifySupabase.rows[0].count} records remaining\n`);

        if (verifyLogs.rows[0].count === '0' && verifySupabase.rows[0].count === '0') {
            console.log('🎉 All history data cleared successfully!');
            console.log('📝 Note: Refresh your browser with Ctrl+Shift+R (hard refresh)');
        } else {
            console.log('⚠️  Warning: Some data may still remain');
        }

    } catch (err) {
        console.error('❌ Error:', err.message);
        console.error(err.stack);
    } finally {
        await client.end();
    }
}

clearAllHistoryData();
