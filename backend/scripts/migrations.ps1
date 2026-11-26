<#
.SYNOPSIS
    Manages the EF Core migrations for both database providers (PostgreSQL and MySQL/MariaDB).

.DESCRIPTION
    Until launch, each provider has exactly one migration, InitialCreate. After changing the model,
    regenerate it for both providers instead of adding new migrations, then recreate local databases
    (docker compose down -v).

    Works in Windows PowerShell 5.1 and PowerShell 7 (pwsh), also on Linux, so CI can use it as is.
    No database is needed for -Regenerate and -Check.

.EXAMPLE
    ./scripts/migrations.ps1
    Deletes both migration sets and regenerates InitialCreate for each provider.

.EXAMPLE
    ./scripts/migrations.ps1 -Check
    Fails if the model has changes that are not in the migrations, for either provider.

.EXAMPLE
    ./scripts/migrations.ps1 -Apply -Provider MySql -ServerVersion 10.4.12-mariadb -Connection "Server=...;Database=...;User=...;Password=..."
    Applies the migrations of one provider to the given database.
#>
[CmdletBinding(DefaultParameterSetName = 'Regenerate')]
param(
    [Parameter(ParameterSetName = 'Check', Mandatory = $true)]
    [switch]$Check,

    [Parameter(ParameterSetName = 'Apply', Mandatory = $true)]
    [switch]$Apply,

    [Parameter(ParameterSetName = 'Apply', Mandatory = $true)]
    [ValidateSet('Postgres', 'MySql')]
    [string]$Provider,

    [Parameter(ParameterSetName = 'Apply', Mandatory = $true)]
    [string]$Connection,

    # MySql only, e.g. 10.4.12-mariadb or 8.0.36-mysql. Defaults to the oldest supported server.
    [Parameter(ParameterSetName = 'Apply')]
    [string]$ServerVersion
)

$ErrorActionPreference = 'Stop'

$backend = Split-Path -Parent $PSScriptRoot
$project = Join-Path $backend 'HotelReservation.Api'

$providers = @(
    @{ Name = 'Postgres'; Context = 'PostgresAppDbContext'; Folder = 'Migrations/Postgres' },
    @{ Name = 'MySql';    Context = 'MySqlAppDbContext';    Folder = 'Migrations/MySql' }
)

function Invoke-DotnetEf {
    param([string[]]$EfArgs)
    & dotnet ef @EfArgs --project $project --startup-project $project
    if ($LASTEXITCODE -ne 0) { throw "dotnet ef $($EfArgs -join ' ') failed with exit code $LASTEXITCODE." }
}

Push-Location $backend
try {
    # Uses the dotnet-ef version pinned in backend/dotnet-tools.json.
    & dotnet tool restore | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'dotnet tool restore failed.' }

    # Regenerate deletes the old migrations before building, so they never need to compile.
    if ($PSCmdlet.ParameterSetName -eq 'Regenerate') {
        foreach ($p in $providers) {
            $folder = Join-Path $project $p.Folder
            if (Test-Path $folder) { Remove-Item $folder -Recurse -Force }
        }
    }

    & dotnet build $project --nologo --verbosity quiet
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }

    switch ($PSCmdlet.ParameterSetName) {
        'Regenerate' {
            foreach ($p in $providers) {
                Write-Host "Generating InitialCreate for $($p.Name)..."
                Invoke-DotnetEf @('migrations', 'add', 'InitialCreate', '--context', $p.Context, '--output-dir', $p.Folder, '--no-build')
            }
            Write-Host 'Done. Recreate local databases with: docker compose down -v'
        }

        'Check' {
            foreach ($p in $providers) {
                Write-Host "Checking $($p.Name) migrations against the model..."
                try {
                    Invoke-DotnetEf @('migrations', 'has-pending-model-changes', '--context', $p.Context, '--no-build')
                }
                catch {
                    throw "The $($p.Name) migrations are out of date. Regenerate them with ./scripts/migrations.ps1 (don't add a new migration)."
                }
            }
            Write-Host 'Migrations match the model for every provider.'
        }

        'Apply' {
            $p = $providers | Where-Object { $_.Name -eq $Provider }
            # Read by MySqlDesignTimeFactory; ignored for Postgres.
            $previous = $env:Database__ServerVersion
            if ($ServerVersion) { $env:Database__ServerVersion = $ServerVersion }
            try {
                Write-Host "Applying $Provider migrations..."
                Invoke-DotnetEf @('database', 'update', '--context', $p.Context, '--connection', $Connection, '--no-build')
            }
            finally {
                $env:Database__ServerVersion = $previous
            }
        }
    }
}
finally {
    Pop-Location
}
