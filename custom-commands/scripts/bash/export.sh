#!/usr/bin/env bash

usage() {
    echo "Exports a Custom Commands application."
    echo "-r | --region             Region (i.e. westus)"
    echo "-s | --subscriptionkey    Speech subscription key"
    echo "-l | --language           Language of the application (i.e. en-us)"
    echo "-a | --appid              Id of the application"
    echo "-f | --file               File path to save the dialog model"
}

while [ "$1" != "" ]; do
    case $1 in
        -r | --region ) shift
            region=$1
            ;;
        -s | --subscriptionkey ) shift
            subscriptionkey=$1
            ;;
        -l | --language ) shift
            language=$1
            ;;
        -a | --appid ) shift
            appid=$1
            ;;
        -f | --file ) shift
            file=$1
            ;;
        -h | --help ) 
            usage
            exit
            ;;
         * )
            usage
            exit 1
    esac
    shift
done

url="https://${region}.commands.speech.microsoft.com/v1.0/apps/${appid}/slots/default/languages/${language}/model?excludeModelMetadata=true"
authHeader="Ocp-Apim-Subscription-Key: $subscriptionkey"

curl -s $url -H "$authHeader" -o "$file"
result=$(cat ${file})

if [[ "$result" == *"error"* ]]; 
then
    echo "Error while exporting: $result"
    exit 1
fi

echo "$result"
