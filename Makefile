worker:
	dotnet run --project ./cryptotracker.worker

web:
	dotnet run --project ./cryptotracker.webapi

docker_build_web:
	docker buildx build --target final . -t janmer/cryptotracker_web:dev

docker_build_web_final:
	docker buildx build --target final . -t janmer/cryptotracker_web:latest
	
docker_push_web:
	docker push janmer/cryptotracker_web:dev
	
docker_push_web_final:
	docker push janmer/cryptotracker_web:latest

docker_build_worker:
	docker buildx build --target final . -f ./cryptotracker.worker/Dockerfile -t janmer/cryptotracker_worker:dev

docker_build_worker_final:
	docker buildx build --target final . -f ./cryptotracker.worker/Dockerfile -t janmer/cryptotracker_worker:latest
	
docker_push_worker:
	docker push janmer/cryptotracker_worker:dev

docker_push_worker_final:
	docker push janmer/cryptotracker_worker:latest