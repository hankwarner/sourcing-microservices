# Readme

[TOC]

* project: ServiceSourcing
* author: Hank Warner 
* email: <h.warner@supply.com>
* group: netsuite
* created on: 2020.01.08
* generated with template version 1.14
* external server names:
    * dev: 
        * onprem: <https://dev-service-servicesourcing.supply.com>
        * gcp: <https://dev-service-servicesourcing.nbsupply.net>
    * prod: 
        * onprem: <https://service-servicesourcing.supply.com>
        * gcp: <https://service-servicesourcing.nbsupply.net>

## Post generation

After generating the project:

### Setup git repo

1. Initialize git:
    ```bash 
    git init
    git add .
    git commit -am "Initial commit"
    ```
2. Create a BitBucket repository and push this project into it:
    * Owner: `supplydev`
    * Project: `netsuite`
    * Repository name: `servicesourcing`
3. Push code to repo 
    ```bash
    git remote add origin git@bitbucket.org:supplydev/servicesourcing.git
    git push -u origin master
    ```

### Connect to Auth0

To add authentication/authorization security to your servicesourcing, do
the following:

1. Create Auth0 Application
    1. Go to
        [Applications](https://manage.auth0.com/dashboard/us/dev-l9q6un7s/applications)
    2. click `Create Application`
        1. Name: `ServiceSourcing`
        2. if unsure, pick `Regular Web Applications`
    4. Under `Settings` tab
        1. set callback URLs to: `http://localhost:5485/oauth_callback,
            https://localhost:5001/oauth_callback,
            https://dev-service-servicesourcing.supply.com/oauth_callback,
            https://service-servicesourcing.supply.com/oauth_callback`
    5. Click `Save Changes`
2. Create Auth0 API
    1. Go to [APIs](https://manage.auth0.com/dashboard/us/dev-l9q6un7s/apis)
    2. click `Create API`
        1. Name: `ServiceSourcing`
        2. Identifier: `https://service-servicesourcing.supply.com`
        3. Signing Algorithm: `RS256`
        4. Click Create
    3. On `Settings` tab
        3. Enable `RBAC`
        4. Enable `Add Permissions in the Access Token`
        5. Enable `Allow Skipping User Consent`
        6. Enable `Allow Offline Access`
    4. On `Machine to Machine Applications` tab
        1. Authorize `ServiceSourcing` 
3. Configure code
    1. in `ServiceSourcing/StartUp.cs` (Auth0 -> Applications ->
        ServiceSourcing -> Settings page): 
        1. Set `Auth0ClientId` to `Client ID`
        2. Set `Auth0ClientSecret` to `Client Secret`
        3. Set `Auth0Deomain` to `Domain`
        
### Configure TeamCity

Go to <https://teamcity.nbsupply.net>

1. Create the VCS Root.
    * Navigate to `Administration`
    * Click `<Root project>`
    * Click `VCS Roots` from leftnav
    * Click `+Create VCS Root`
    * Type of VCS: `Git`
    * VCS root name: `ServiceSourcing VCS`
    * VCS root ID: `ServiceSourcing_ServiceSourcingVcs`
    * Fetch Url: `git@BITBUCKETPROJECTURL`
    * Default branch: `refs/heads/master`
    * Branch specification:
        ```
        refs/heads/*
        refs/tags/*
        ```
    * Use tags as branches: `check`
    * Authentication method: `Default Private Key`
    * Click `Test Connection`
    * If connection is successful, click `Create`
2. Create project
    * Navigate to `Administration`.  
    * Click `<Root project> -> ServiceSourcings`
    * Click `+Create subproject`.
    * Go to `From Bitbucket Cloud` tab. Select bitbucket project from list.
    * Use git url; eg) `git@bitbucket.org:supplydev/demo_project.git`
        * leave username/password blank 
    * Select `Import settings from .teamcity/settings.kts` and click
        `Proceed`
3. Navigate to created Project: `Projects -> ServiceSourcing`
    * Click `Edit Project`
    * Click `Versioned Settings`
        * select `Synchronization enabled`
        * set `Project settings VCS root` to `ServiceSourcing VCS`
        * set `Settings format` to `Kotlin`
        * Click Apply
        * Click `Import settings from VCS`

### Update Terraform

#### Dev stack

To provision resources in GCP on the dev stack, add an entry for the
service name to the `dev-services` array in `supplydev/main.tf`.

#### Prod stack

TBA

### Connect to SupplyID

TODO: add instructions on how to configure add an application entry to
the SupplyIDP data store; update client id and client secret.

### Setup DNS entries

Add DNS entries to GCP terraform project for 

* <https://dev-service-servicesourcing.nbsupply.net>
    * point to one of the dev-servers in `supplydev` project
* <https://service-servicesourcing.nbsupply.net>
    * point to load balancer in `supplyprod` project

### Setup PostGreSQL

#### Provision databases

##### Dev stack

1. Database will be provisioned during `Update Terraform` step (see
    below)
2. After database is provisioned, get login credentials from devops team
    and persist to 
    * `csharp/ServiceSourcing/appsettings.development.json`.
    * `ansible/dev/systemd.service`.

##### Prod stack

1. Add servicesourcing name to terraform `supplyprod`
2. After database is provisioned, get login credentials from devops team
    and persist to 
    * `csharp/ServiceSourcing/appsettings.production.json`.
    * `ansible/prod/systemd.service`.


#### Database migration

To install prereqs, add migrations and migrate:

```powershell
# install Entity Framework Core tools globally
dotnet tool install --global dotnet-ef --version 2.2

# install Entity Framework Core tools locally 
dotnet new tool-manifest
dotnet tool install dotnet-ef --version 2.2

# dotnet tool restore may be required:
dotnet tool restore

# initialize migrations (template has already done this)
dotnet ef migrations add InitialCreate

# generate new migrations with  
dotnet ef migrations add MigrationName

# run pending migrations
dotnet ef database update
```

To migrate, execute the `csharp/ServiceSourcing.Migrate` project. Do not 
migrate from `Startup.cs`, because horizontally scaled servicesourcings
executing concurrent schema changes will cause problems.

See
<https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/>
for more information.

##### Migrate via TeamCity

The TeamCity config now contains jobs to migrate dev and prod databases;
this is the preferred way to migrate.

## Deployment process

Deployments to GCP are automated through teamcity.  
 
To deploy, go to <https://teamcity.nbsupply.net>

### One-time steps for production

1. Create DNS entry for `service-servicesourcing.nbsupply.net` in terraform
   project.
    * point to loadbalancer IP
3. 

## Legacy Deployment process

Deployments to on-prem, in IIS are a largely manual process. Follow 
these instructions to deploy a servicesourcing on-prem.

### Deploy to IIS

#### dotnet publish

The IIS instructions must be done for:  

* `srv-pro-iisa-01`
* `srv-pro-iisa-02`

#### publish project files to servers

1. Create new folders:
    1. `\\srv-pro-iisa-01\inetpub\servicesourcing`
    2. `\\srv-pro-iisa-02\inetpub\servicesourcing`
2. Publish project to new folder (from `csharp/ServiceSourcing`:

    ```powershell
    echo "" > \\srv-pro-iisa-01\inetpub\servicesourcing\app_offline.htm
    dotnet clean
    dotnet publish -c Release -o \\srv-pro-iisa-01\inetpub\servicesourcing -r win-x64 --self-contained=false
    del \\srv-pro-iisa-01\inetpub\servicesourcing\app_offline.htm
    
    echo "" > \\srv-pro-iisa-02\inetpub\servicesourcing\app_offline.htm
    dotnet clean
    dotnet publish -c Release -o \\srv-pro-iisa-02\inetpub\servicesourcing -r win-x64 --self-contained=false
    del \\srv-pro-iisa-02\inetpub\servicesourcing\app_offline.htm
    ```

#### create new iis site

New site setup:

* Site name: `ServiceSourcing`
* Physical path: `C:\inetpub\servicesourcing`
* Application pool: `ServiceSourcing`
* Use `Http`
* IP Address: `All Unassigned`
* Host name: `service-servicesourcing.supply.com`

Application pool setup:

* Go to `ServiceSourcing` application pool
    * Under `Advanced Settings`:
        * General -> .NET CLR Version: `No Managed Code`
        * General -> Start Mode: `AlwaysRunning`
        * Process Model -> Identity: `nbsupply\servicesourcing`
            * can use `nbsupply\r.nixon` for beta releases
        * Load User Pofile: `true`
        * Process Model -> Idle Time-out (minutes): `0`
        * Recycling -> Regular Time Interval (minutes): `0`

Set environment variables:

* Go to `ServiceSourcing` site -> Configuration Editor (under Management
section)
* Section: `system.webserver/aspNetCore`
* From: `ApplicationHost.config <location path='ServiceSourcing' />`
* Manually copy over each environment variable 
    * Make sure to hit Apply in Configuration Editor after you set your
        environment variables
* Add environment variable for New Relic configuration:
    * `NEW_RELIC_APP_NAME` = `ServiceSourcing` 

### HA Proxy Setup

To setup HA Proxy, navigate to <https://guardian.nbsupply.com:8888/>. Go
to services. Use your nbsupply credentials to log in, without the
`nbsupply\` prefix in your username. Go to `Services` -> `HAProxy`.

#### backend setup

1. Go to `Backend`
2. Scroll to bottom and click `Add`
    * under `Edit HAProxy Backend server pool`
        * Name: `Service-ServiceSourcing-IISA` 
        * Server list (add entries):
            * Name: `IISA-01`
                * Forwardto: `Address+Port`
                * Address: `10.0.20.123`
                * Port: `80`
            * Name: `IISA-02`
                * Forwardto: `Address+Port`
                * Address: `10.0.20.124`
                * Port: `80`
    * under `Loadbalancing options`
        * check `Round Robin`
    * under `Timeout / retry settings`
        * Connection timeout: leave blank
        * Server timeout: leave blank
        * Retries: `3`
    * under `Health checking`
        * Health check method: `HTTP`
        * Check frequency: `5485`
        * Log checks: unchecked
        * Http check method: `GET`
        * url used by http check requests:
            `http://service-servicesourcing.supply.com/health`
3. Click Save 
4. Click Apply Changes

#### frontend setup

1. Go to `Frontend`
2. Edit `Frontend-2-re-encryption`
    * under `Default backend, access control lists and actions`:
        * under `Access Control lists`:
            * click `add another entry` button
                * Name: `ServiceSourcingACL` 
                * Expression: `Host matches:`
                * Value: `service-servicesourcing.supply.com`
        * under `Actions`
            * click `add another entry`
                * Action: `Use Backend`
                * Condition acl names: `ServiceSourcingACL`
                * backend: `Service-ServiceSourcing-IISA`
    * under `Advanced settings`: 
        * Use "forwardfor" option: `check`
3. Click `Save` at bottom of page
4. Click Apply Changes

### DNS Setup

Temporarily: set manual hosts file

### End Release

Keep release open until you are satisfied with the code that's been
posted to production.

Once you are satisfied:

1. commit remaining changes to `release/1.0` branch
2. run `GitVersion.exe` to update version on release branch
3. make a final version bump commit
    * this commit just saves updates to `AssemblyInfo.cs` files which
        were made by running `GitVersion.exe`
4. merge release branch with `master`
5. immediately tag merged commit with `1.0` 
6. push branch + tag

Project has now been released.

## Helpful stuff

* Tool for converting JSON objects into C# POCO definitions:
  <https://app.quicktype.io/p>
    * use lists instead of arrays
    * turn off enum generation