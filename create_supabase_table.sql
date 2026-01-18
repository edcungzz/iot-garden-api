-- Create sensor_logs table in Supabase
-- Run this in Supabase SQL Editor

-- Drop table if exists (optional - use only if you want to recreate)
-- DROP TABLE IF EXISTS sensor_logs CASCADE;

-- Create sensor_logs table
CREATE TABLE IF NOT EXISTS sensor_logs (
    id BIGSERIAL PRIMARY KEY,
    device_id TEXT,
    temp FLOAT4,
    humi FLOAT4,
    ec INTEGER,
    ph FLOAT4,
    n INTEGER,
    p INTEGER,
    k INTEGER,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
);

-- Create index for better query performance
CREATE INDEX IF NOT EXISTS idx_sensor_logs_created_at ON sensor_logs(created_at DESC);
CREATE INDEX IF NOT EXISTS idx_sensor_logs_device_id ON sensor_logs(device_id);
CREATE INDEX IF NOT EXISTS idx_sensor_logs_device_created ON sensor_logs(device_id, created_at DESC);

-- Enable Row Level Security (RLS) - optional
ALTER TABLE sensor_logs ENABLE ROW LEVEL SECURITY;

-- Create policy to allow all operations (you can restrict this later)
CREATE POLICY "Allow all operations on sensor_logs" 
ON sensor_logs 
FOR ALL 
USING (true) 
WITH CHECK (true);

-- Verify table was created
SELECT 
    column_name, 
    data_type,
    is_nullable
FROM information_schema.columns 
WHERE table_name = 'sensor_logs'
ORDER BY ordinal_position;

-- Show sample data (will be empty at first)
SELECT COUNT(*) as total_records FROM sensor_logs;
