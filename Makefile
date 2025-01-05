worker:
	dotnet run --project ./cryptotracker.worker

web:
	dotnet run --project ./cryptotracker.webapi

docker_build_worker:
	docker buildx build . -f ./cryptotracker.worker/.dockerfile -t janmer/cryptotracker_worker:dev

docker_build_worker_final:
	docker buildx build --target final . -f ./cryptotracker.worker/.dockerfile -t janmer/cryptotracker_worker:latest
	
docker_push_worker:
	docker push janmer/cryptotracker_worker:dev

docker_push_worker_final:
	docker push janmer/cryptotracker_worker:latest