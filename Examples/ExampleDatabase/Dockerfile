# Stage 1 - Build the dacpac file
FROM microsoft/mssql-tools as deps

LABEL project=example_database

RUN apt-get update && apt-get install dos2unix unzip curl apt-transport-https software-properties-common locales libunwind-dev -y && \
curl https://go.microsoft.com/fwlink/?linkid=873926 -L -o sqlpackage.zip && \
echo DOTNET_CLI_TELEMETRY_OPTOUT=1 >> /etc/environment && \
locale-gen en_US.UTF-8 && printf 'LANGUAGE=en_US.UTF-8\nLC_ALL=en_US.UTF-8\n' >> /etc/default/locale && \
mkdir -p /opt/mssql/sqlpackage && \
unzip sqlpackage.zip -d /opt/mssql/sqlpackage && \
chmod a+x /opt/mssql/sqlpackage/sqlpackage && \
ln -s /opt/mssql/sqlpackage/sqlpackage /usr/bin/sqlpackage && \
ln -s /opt/mssql-tools/bin/sqlcmd /usr/bin/sqlcmd
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

FROM deps AS build
WORKDIR /app
# If we eventually get SSDT for linux then we can build the dacpac here
COPY . src
# When running locally the dacpac is in bin/debug/ or bin/release
# When published all of the artifacts are in the same folder
RUN cp `ls -t src/*.dacpac 2>/dev/null || ls -t src/*/*/*.dacpac | head -n 1` deploy.dacpac
RUN cp src/*.publish.xml . 2>/dev/null; exit 0
RUN rm -Rf src

FROM build AS final
RUN ls deploy.dacpac > /dev/null # error if file doesnt exist

# Stage 2 - Build the sql server and apply the newly created dacpac file
FROM microsoft/mssql-server-linux:2017-latest as deploy
# Install Unzip
RUN apt-get update \
    && apt-get install unzip -y
# Install SQLPackage for Linux and make it executable
RUN wget -progress=bar:force -q -O sqlpackage.zip https://go.microsoft.com/fwlink/?linkid=873926 \
    && unzip -qq sqlpackage.zip -d /opt/sqlpackage \
    && chmod +x /opt/sqlpackage/sqlpackage
# Add the DACPAC to the image
COPY --from=build /app/deploy.dacpac .

ARG DATABASE
ARG PASSWORD

# Configure the required environmental variables
ENV ACCEPT_EULA=Y
ENV SA_PASSWORD=$PASSWORD
# Launch SQL Server, confirm startup is complete, deploy the DACPAC, then terminate SQL Server.
# See https://stackoverflow.com/a/51589787/488695
RUN ( /opt/mssql/bin/sqlservr & ) | grep -q "Service Broker manager has started" \
    && /opt/sqlpackage/sqlpackage /a:Publish /tsn:. /tdn:${DATABASE} /tu:sa /tp:${PASSWORD} /sf:/deploy.dacpac \
    && rm deploy.dacpac \
    && pkill sqlservr