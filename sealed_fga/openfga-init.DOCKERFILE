FROM alpine:latest

COPY --from=openfga/cli:latest /fga /bin/fga
COPY --from=openfga/openfga /openfga /bin/openfga
COPY SealedFga.Sample/model.fga /model.fga

RUN apk add --no-cache jq

COPY --chmod=755 <<EOF /run.sh
#!/bin/sh
set -x
/bin/openfga run | tee openfga.log &

echo "Waiting for running"
until nc -z localhost 8080; do
  echo "Waiting for OpenFGA service..."
  sleep 1
done

echo "Applying migration"

STORE_ID=\$(fga store list | jq -re .stores[0].id)
if [ \$? -ne 0 ];
then
  fga store create --name sfga --model /model.fga
  exit 0 # done
fi

# just install a new model, if it's the same nothing should happen™️
fga model write --store-id \$STORE_ID --file /model.fga
EOF

ENTRYPOINT [ "/run.sh" ]

