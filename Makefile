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

ef_remove_migration:
	dotnet ef migrations remove  --project cryptotracker.database --startup-project cryptotracker.webapi

ef_update_database:
	dotnet ef database update --project cryptotracker.database --startup-project cryptotracker.webapi

ef_add_migration:
	@echo "Adding migration: $(filter-out $@,$(MAKECMDGOALS))"
	dotnet ef migrations add $(filter-out $@,$(MAKECMDGOALS)) --project cryptotracker.database --startup-project cryptotracker.webapi

# Prevent Make from treating the parameter as a target
%:
	@:
