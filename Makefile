#!make

clean:
	-cd csharp; \
		dotnet clean;
	
# compilation between windows/wsl can cause errors in the IDE; resolve w/the scrub make target
platform-clean:
	-rm -r csharp/ServiceSourcing/bin
	-rm -r csharp/ServiceSourcing/obj
	-rm -r csharp/ServiceSourcing.Debug/bin
	-rm -r csharp/ServiceSourcing.Debug/obj
	-rm -r csharp/ServiceSourcing.FunctionalTest/bin
	-rm -r csharp/ServiceSourcing.FunctionalTest/obj
	-rm -r csharp/ServiceSourcing.UnitTest/bin
	-rm -r csharp/ServiceSourcing.UnitTest/obj

scrub: platform-clean clean

build:
	cd csharp; \
		dotnet build;

run:
	cd csharp/ServiceSourcing; \
		dotnet run;

test: build
	cd csharp; \
		dotnet test;

publish_dev:
	cd csharp/ServiceSourcing; \
		dotnet publish -c Debug -r ubuntu.18.04-x64 --self-contained=true

publish_prod:
	cd csharp/ServiceSourcing; \
		dotnet publish -c Release -r ubuntu.18.04-x64 --self-contained=true

csharp/ServiceSourcing/bin/Debug/netcoreapp2.2/ubuntu.18.04-x64/publish: publish_dev

csharp/ServiceSourcing/bin/Release/netcoreapp2.2/ubuntu.18.04-x64/publish: publish_prod

deploy_dev: csharp/ServiceSourcing/bin/Debug/netcoreapp2.2/ubuntu.18.04-x64/publish
	cd ansible; \
		ansible-playbook -i inventory dev/deploy.yml

deploy_prod: csharp/ServiceSourcing/bin/Release/netcoreapp2.2/ubuntu.18.04-x64/publish
	cd ansible; \
		ansible-playbook -i inventory prod/deploy.yml
