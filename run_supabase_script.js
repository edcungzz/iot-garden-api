const { Client } = require('pg');
const fs = require('fs');
const path = require('path');

// Supabase connection
const client = new Client({
    host: 'aws-1-ap-southeast-1.pooler.supabase.com',
    port: 6543,
    database: 'postgres',
    user: 'postgres.fjljkiwiobhbazzqusie',
    password: 'osWDtHrdSWQ96Z29',
    ssl: false
});

async function runSupabaseScript() {
    try {
        await client.connect();
        console.log('🔄 Connected to Supabase');

        // Read SQL file
        const sqlPath = path.join(__dirname, 'supabase_add_device_id.sql');
        const sql = fs.readFileSync(sqlPath, 'utf8');

        console.log('📋 Running SQL script...');
        const result = await client.query(sql);

        console.log('✅ SQL script executed successfully!');
        console.log('Result:', result);

        // Verify column was added
        const verifyQuery = `
            SELECT column_name, data_type 
            FROM information_schema.columns 
            WHERE table_name = 'sensor_logs'
            ORDER BY ordinal_position;
        `;

        const columns = await client.query(verifyQuery);
        console.log('\n📊 Current sensor_logs columns:');
        columns.rows.forEach(col => {
            console.log(`  - ${col.column_name} (${col.data_type})`);
        });

    } catch (error) {
        console.error('❌ Error:', error.message);
        console.error('Details:', error);
    } finally {
        await client.end();
        console.log('\n✅ Done!');
    }
}

runSupabaseScript();
