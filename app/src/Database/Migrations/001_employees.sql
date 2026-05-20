CREATE SCHEMA IF NOT EXISTS zeloshr;

CREATE TABLE IF NOT EXISTS zeloshr.zhr_employees (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    employee_code VARCHAR(32) NOT NULL,
    tenant_id VARCHAR(128) NOT NULL,
    org_id VARCHAR(128) NOT NULL,
    first_name VARCHAR(100) NOT NULL,
    middle_name VARCHAR(100),
    last_name VARCHAR(100) NOT NULL,
    date_of_birth DATE NOT NULL,
    gender VARCHAR(50) NOT NULL,
    nationality VARCHAR(100) NOT NULL,
    ghana_card_number VARCHAR(50) NOT NULL,
    personal_email VARCHAR(255) NOT NULL,
    personal_phone VARCHAR(50) NOT NULL,
    residential_address TEXT NOT NULL,
    ghana_post_gps VARCHAR(100) NOT NULL,
    lifecycle_state VARCHAR(50) NOT NULL DEFAULT 'Pre-hire',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_zhr_employees_code UNIQUE (employee_code),
    CONSTRAINT uq_zhr_employees_ghana_card UNIQUE (tenant_id, ghana_card_number)
);

CREATE INDEX IF NOT EXISTS ix_zhr_employees_tenant_org
    ON zeloshr.zhr_employees (tenant_id, org_id);

CREATE INDEX IF NOT EXISTS ix_zhr_employees_name
    ON zeloshr.zhr_employees (tenant_id, org_id, last_name, first_name);
