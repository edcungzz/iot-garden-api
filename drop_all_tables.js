const { Client } = require('pg');

const client = new Client({
    host: 'aws-1-ap-southeast-1.pooler.supabase.com',
    port: 6543,
    database: 'postgres',
    user: 'postgres.fjljkiwiobhbazzqusie',
    password: 'osWDtHrdSWQ96Z29',
    ssl: false
});

async function dropAllTables() {
    try {
        await client.connect();
        console.log('🔄 Connected to database');

        console.log('🗑️  Dropping old tables...');
        await client.query('DROP TABLE IF EXISTS "SensorLogs" CASCADE');
        await client.query('DROP TABLE IF EXISTS "Sensors" CASCADE');
        await client.query('DROP TABLE IF EXISTS "Devices" CASCADE');
        await client.query('DROP TABLE IF EXISTS "Alerts" CASCADE');
        await client.query('DROP TABLE IF EXISTS "SensorLogData" CASCADE');
        await client.query('DROP TABLE IF EXISTS "__EFMigrationsHistory" CASCADE');

        console.log('✅ All tables dropped successfully!');
        console.log('Now run: dotnet ef database update');
    } catch (error) {
        console.error('❌ Error:', error.message);
    } finally {
        await client.end();
    }
}

dropAllTables();
