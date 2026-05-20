-- Branches
CREATE TABLE IF NOT EXISTS zeloshr.zhr_branches (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id VARCHAR(128) NOT NULL,
    org_id VARCHAR(128) NOT NULL,
    name VARCHAR(150) NOT NULL,
    is_archived BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_zhr_branches_name UNIQUE (tenant_id, org_id, name)
);

-- Departments (hierarchical)
CREATE TABLE IF NOT EXISTS zeloshr.zhr_departments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id VARCHAR(128) NOT NULL,
    org_id VARCHAR(128) NOT NULL,
    name VARCHAR(150) NOT NULL,
    parent_department_id UUID REFERENCES zeloshr.zhr_departments(id),
    head_of_department_id UUID,
    is_archived BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_zhr_departments_name UNIQUE (tenant_id, org_id, name)
);

-- Employee directory fields
ALTER TABLE zeloshr.zhr_employees ADD COLUMN IF NOT EXISTS job_title VARCHAR(150);
ALTER TABLE zeloshr.zhr_employees ADD COLUMN IF NOT EXISTS department_id UUID REFERENCES zeloshr.zhr_departments(id);
ALTER TABLE zeloshr.zhr_employees ADD COLUMN IF NOT EXISTS branch_id UUID REFERENCES zeloshr.zhr_branches(id);
ALTER TABLE zeloshr.zhr_employees ADD COLUMN IF NOT EXISTS employment_type VARCHAR(50);
ALTER TABLE zeloshr.zhr_employees ADD COLUMN IF NOT EXISTS manager_id UUID;
ALTER TABLE zeloshr.zhr_employees ADD COLUMN IF NOT EXISTS employment_status VARCHAR(50) NOT NULL DEFAULT 'Active';
ALTER TABLE zeloshr.zhr_employees ADD COLUMN IF NOT EXISTS contract_type VARCHAR(50) DEFAULT 'Permanent';
ALTER TABLE zeloshr.zhr_employees ADD COLUMN IF NOT EXISTS probation_end_date DATE;
ALTER TABLE zeloshr.zhr_employees ADD COLUMN IF NOT EXISTS employment_start_date DATE;
ALTER TABLE zeloshr.zhr_employees ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;

CREATE INDEX IF NOT EXISTS ix_zhr_employees_directory
    ON zeloshr.zhr_employees (tenant_id, org_id, employment_status, department_id, branch_id);

CREATE INDEX IF NOT EXISTS ix_zhr_employees_search
    ON zeloshr.zhr_employees (tenant_id, org_id, last_name, first_name, employee_code);
