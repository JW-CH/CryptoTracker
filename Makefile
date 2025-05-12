old_worker:
	dotnet run --project ./cryptotracker.worker

web:
	dotnet run --project ./cryptotracker.webapi

docker_build_web_dev:
	docker buildx build --target final . -t janmer/cryptotracker_web:dev

docker_build_web:
	docker buildx build --target final . -t janmer/cryptotracker_web:latest
	
docker_push_web_dev:
	docker push janmer/cryptotracker_web:dev
	
docker_push_web:
	docker push janmer/cryptotracker_web:latest

ef_remove_migration:
	dotnet ef migrations remove  --project cryptotracker.database --startup-project cryptotracker.webapi

ef_update_database:
	dotnet ef database update $(filter-out $@,$(MAKECMDGOALS)) --project cryptotracker.database --startup-project cryptotracker.webapi

ef_add_migration:
	@echo "Adding migration: $(filter-out $@,$(MAKECMDGOALS))"
	dotnet ef migrations add $(filter-out $@,$(MAKECMDGOALS)) --project cryptotracker.database --startup-project cryptotracker.webapi

# Prevent Make from treating the parameter as a target
%:
	@:
