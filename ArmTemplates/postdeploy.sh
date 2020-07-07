#!/bin/bash

# Set SQL DB Firewall rules
OUTBOUNDIP=$(az webapp show -n $WEBAPPNAME-backend -g $RESOURCE_GROUP -o tsv --query 'outboundIpAddresses')
OTHEROUTBOUNDIP=$(az webapp show -n $WEBAPPNAME-backend -g $RESOURCE_GROUP -o tsv --query 'possibleOutboundIpAddresses')

# TODO - In production, we should be deleting firewall rules and recreating. For now, we
# aren't deleting so developers have access to the db
IFS=', \t\n' read -ra ADDR <<< "$OUTBOUNDIP,$OTHEROUTBOUNDIP"
uniqip=($(printf "%s\n" "${ADDR[@]}" | sort -u))
for ip in "${uniqip[@]}"; do
    az sql server firewall-rule create -g $RESOURCE_GROUP \
    --server ${TAG}sqlsvr \
    -n IP_$ip --start-ip-address $ip --end-ip-address $ip
done
IFS=' \t\n'