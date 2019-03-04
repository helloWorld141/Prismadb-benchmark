Push-Location $PSScriptRoot

try {
    $DigitalOceanToken = $env:DOTokenSecure
    $DockerMachine = 'BenchmarkTest'
	
    Set-Location PrismaBenchmark | dotnet restore | dotnet publish -c Release -o out

    docker-machine create --driver digitalocean --digitalocean-access-token $DigitalOceanToken `
        --digitalocean-region='nyc3' --digitalocean-size='c-32' $DockerMachine
	
    docker-machine ls
    docker-machine env --shell powershell $DockerMachine
    docker-machine env --shell powershell $DockerMachine | Invoke-Expression
    #see which is active
    docker-machine active
	
    docker load -i "$PSScriptRoot/prismadb-proxy-mssql.tar"
	
    docker-compose up -d --build prismadb prismaproxy
    Start-Sleep -s 300
    docker-compose up --build prismabenchmark
}
finally {
    if ($LastExitCode -ne 0) { $host.SetShouldExit($LastExitCode) }
    Pop-Location
}