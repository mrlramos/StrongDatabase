@echo off
REM Script para ajustar replicação após docker-compose up

REM Aguarda o primary-db subir
:waitloop
for /f "tokens=1" %%i in ('docker ps --filter "name=primary-db" --filter "status=running" --format "{{.ID}}"') do set CONTAINER=%%i
if not defined CONTAINER (
    echo Aguardando o primary-db iniciar...
    timeout /t 2 >nul
    goto waitloop
)

REM Copia o pg_hba.conf customizado (caminho absoluto)
copy /Y "%~dp0..\docker\primary\pg_hba.conf" "%~dp0pg_hba.conf"

docker cp "%~dp0pg_hba.conf" primary-db:/var/lib/postgresql/data/pg_hba.conf

del "%~dp0pg_hba.conf"

REM Reinicia o primary-db para aplicar o novo pg_hba.conf
docker restart primary-db

REM Reinicia as réplicas e standby para tentarem novamente a replicação
docker-compose restart standby-db replica1-db replica2-db

echo Ambiente de replicação ajustado com sucesso!
pause 