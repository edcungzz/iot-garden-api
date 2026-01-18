const { Client } = require('pg');

const connectionString = "postgresql://postgres.fjljkiwiobhbazzqusie:osWDtHrdSWQ96Z29@aws-1-ap-southeast-1.pooler.supabase.com:6543/postgres";

async function testQuery() {
    const client = new Client({
        connectionString: connectionString,
        ssl: { rejectUnauthorized: false }
    });

    try {
        await client.connect();
        console.log('🔌 Connected to database\n');

        // Test simple query
        const result = await client.query('SELECT COUNT(*) FROM "SensorLogs"');
        console.log(`Total SensorLogs: ${result.rows[0].count}`);

        // Test with timestamp filter
        const now = new Date();
        const cutoff = new Date(now.getTime() - (24 * 60 * 60 * 1000)); // 24 hours ago

        console.log(`\nCurrent time: ${now.toISOString()}`);
        console.log(`Cutoff time: ${cutoff.toISOString()}`);

        const filtered = await client.query(
            'SELECT COUNT(*) FROM "SensorLogs" WHERE "Timestamp" >= $1',
            [cutoff]
        );
        console.log(`\nRecords in last 24 hours: ${filtered.rows[0].count}`);

        // Show sample data
        const sample = await client.query(
            'SELECT "SensorId", "Value", "Timestamp" FROM "SensorLogs" ORDER BY "Timestamp" DESC LIMIT 3'
        );
        console.log('\nSample data:');
        sample.rows.forEach((row, i) => {
            console.log(`${i + 1}. ${row.SensorId}: ${row.Value} at ${row.Timestamp}`);
        });

    } catch (err) {
        console.error('❌ Error:', err.message);
        console.error(err.stack);
    } finally {
        await client.end();
    }
}

testQuery();
