#!make

login:
	fly -t concourseci login -c https://concourseci.nbsupply.net

set-pipelines: unset-pipelines
	yes | fly -t concourseci set-pipeline -c pipeline.yml -p test-branch
	fly -t concourseci unpause-pipeline -p test-branch
	fly -t concourseci trigger-job -j test-branch/unit-test -w

test:
	fly -t concourseci e -c job.yml -i repo=../csharp

unset-pipelines:
	-fly -t tutorial destroy-pipeline -p test-branch -n
