import jetbrains.buildServer.configs.kotlin.v2018_2.*
import jetbrains.buildServer.configs.kotlin.v2018_2.buildFeatures.dockerSupport
import jetbrains.buildServer.configs.kotlin.v2018_2.triggers.vcs

/*
The settings script is an entry point for defining a TeamCity
project hierarchy. The script should contain a single call to the
project() function with a Project instance or an init function as
an argument.

VcsRoots, BuildTypes, Templates, and subprojects can be
registered inside the project using the vcsRoot(), buildType(),
template(), and subProject() methods respectively.

To debug settings scripts in command-line, run the

    mvnDebug org.jetbrains.teamcity:teamcity-configs-maven-plugin:generate

command and attach your debugger to the port 8000.

To debug in IntelliJ Idea, open the 'Maven Projects' tool window (View
-> Tool Windows -> Maven Projects), find the generate task node
(Plugins -> teamcity-configs -> teamcity-configs:generate), the
'Debug' option is available in the context menu for the task.
*/

version = "2019.1"

project {
    description = "https://bitbucket.org/supplydev/servicesourcing/src/master/"

    buildType(DeployToDevelopment)
    buildType(MigrateProductionDatabase)
    buildType(CloseRelease)
    buildType(DeployRelease)
    buildType(TestBranch)
    buildType(OpenRelease)
    buildType(MigrateDevelopmentDatabase)
    buildTypesOrder = arrayListOf(TestBranch, OpenRelease, CloseRelease, MigrateDevelopmentDatabase, DeployToDevelopment, MigrateProductionDatabase, DeployRelease)
}

object CloseRelease : BuildType({
    name = "Close release"

    vcs {
        root(DslContext.settingsRoot)

        cleanCheckout = true
        branchFilter = "+:release*"
    }

    steps {
        step {
            name = "Close release"
            type = "CloseReleaseMeta"
        }
    }

    triggers {
        vcs {
            enabled = false
        }
    }
})

object DeployRelease : BuildType({
    name = "Deploy Release"

    enablePersonalBuilds = false
    maxRunningBuilds = 1

    vcs {
        root(DslContext.settingsRoot)

        branchFilter = """
            +:release*
            +:v*
        """.trimIndent()
    }

    steps {
        step {
            name = "Deploy release"
            type = "DeployReleaseMeta"
            param("DotnetRuntime", "ubuntu.18.04-x64")
            param("env.ASPNETCORE_ENVIRONMENT", "Production")
            param("UnitTestProject", "csharp/ServiceSourcing.UnitTest/ServiceSourcing.UnitTest.csproj")
            param("DotnetConfiguration", "Release")
            param("PublishWorkingDir", "csharp/ServiceSourcing")
            param("DotnetParam", "--self-contained=true")
        }
    }

    triggers {
        vcs {
            enabled = false
        }
    }
})

object DeployToDevelopment : BuildType({
    name = "Deploy to Development"

    vcs {
        root(DslContext.settingsRoot)
    }

    steps {
        step {
            name = "Deploy to development"
            type = "DeployToDevelopmentMeta"
            param("DotnetPublishWorkingDir", "csharp/ServiceSourcing")
            param("DotnetRuntime", "ubuntu.18.04-x64")
            param("env.ASPNETCORE_ENVIRONMENT", "Development")
            param("DotnetConfiguration", "Debug")
            param("DotnetParams", "--self-contained=true")
        }
    }

    triggers {
        vcs {
            enabled = false
        }
    }
})

object MigrateDevelopmentDatabase : BuildType({
    name = "Migrate Development Database"

    vcs {
        root(DslContext.settingsRoot)

        cleanCheckout = true
        branchFilter = ""
    }

    steps {
        step {
            name = "Migrate development database"
            type = "MigrateDevelopmentDatabaseMeta"
            param("DotNetConfiguration", "Debug")
            param("MigrationProject", "csharp/ServiceSourcing.Migrate/ServiceSourcing.Migrate.csproj")
            param("DotNetRuntime", "ubuntu.18.04-x64")
        }
    }

    triggers {
        vcs {
            enabled = false
        }
    }
})

object MigrateProductionDatabase : BuildType({
    name = "Migrate Production Database"

    vcs {
        root(DslContext.settingsRoot)

        branchFilter = """
            +:release*
            +:v*
        """.trimIndent()
    }

    steps {
        step {
            name = "Migrate production database"
            type = "MigrateProductionDatabaseMeta"
            param("DotnetRuntime", "ubuntu.18.04-x64")
            param("MigrationProject", "csharp/ServiceSourcing.Migrate/ServiceSourcing.Migrate.csproj")
            param("env.ASPNETCORE_ENVIRONMENT", "Production")
            param("DotnetConfiguration", "Release")
            param("DotnetParams", "--launch-profile ServiceSourcing.MigrateProd")
        }
    }

    triggers {
        vcs {
            enabled = false
        }
    }
})

object OpenRelease : BuildType({
    name = "Open Release"

    params {
        text("VersionNumber", "", label = "Next version number", description = "version number of next release", display = ParameterDisplay.PROMPT,
                regex = """[0-9]{1,}\.[0-9]{1,}""", validationMessage = "please enter a major/minor version number, eg: 1.17")
    }

    vcs {
        root(DslContext.settingsRoot)

        cleanCheckout = true
        branchFilter = "+:<default>"
    }

    steps {
        step {
            name = "Open release"
            type = "OpenReleaseMeta"
            param("VersionNumber", "%VersionNumber%")
        }
    }

    triggers {
        vcs {
            enabled = false
        }
    }

    features {
        dockerSupport {
        }
    }
})

object TestBranch : BuildType({
    name = "Test Branch"

    vcs {
        root(DslContext.settingsRoot)
    }

    steps {
        step {
            name = "Test branch"
            type = "TestBranchMeta"
            param("UnitTestProject", "csharp/ServiceSourcing.UnitTest/ServiceSourcing.UnitTest.csproj")
        }
    }

    triggers {
        vcs {
            enabled = false
        }
    }
})
