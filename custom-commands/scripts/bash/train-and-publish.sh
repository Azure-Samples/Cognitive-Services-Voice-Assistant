#!/usr/bin/env bash

usage() {
    echo "Trains and Publish a Custom Commands application."
    echo "-r | --region             Region (i.e. westus)"
    echo "-s | --subscriptionkey    Speech subscription key"
    echo "-l | --language           Language of the application (i.e. en-us)"
    echo "-a | --appid              Id of the application"
}

while [[ "$1" != "" ]]; do
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

authHeader="Ocp-Apim-Subscription-Key: $subscriptionkey"

startTrainingUrl="https://${region}.commands.speech.microsoft.com/v1.0/apps/${appid}/slots/default/languages/${language}/train?force=true"
curl -s -I -X POST "$startTrainingUrl" -H "$authHeader" -H "Content-Length: 0" -o StartTrainingResult.json
startTrainingResult=$(cat StartTrainingResult.json)

if [[ "$startTrainingResult" != *"201"* ]]; 
then
    echo "Error while starting training: $startTrainingResult"
    exit 1
fi

idLength=${#startTrainingResult}-13
versionId=$(echo ${startTrainingResult:idLength:10})
echo "Training version: $versionId"

completeTrainingUrl="https://${region}.commands.speech.microsoft.com/v1.0/apps/${appid}/slots/default/languages/${language}/train/${versionId}"
publishUrl="https://${region}.commands.speech.microsoft.com/v1.0/apps/${appid}/slots/default/languages/${language}/publish/${versionId}"

for i in {1..15}
do
    curl -s ${completeTrainingUrl} -H "$authHeader" -o CompleteTrainingResult.json
    completeTrainingResult=$(cat CompleteTrainingResult.json)
    echo "Complete training result: $completeTrainingResult"
    
    if [[ "$completeTrainingResult" == *"error"* ]]; 
    then
        echo "Error while completing training: $completeTrainingResult"
        exit 1
    fi
    if [[ "$completeTrainingResult" == *"Succeeded"* ]]; 
    then
        echo "App trained successfully"
        
        curl -s -X PUT ${publishUrl} -H "$authHeader" -H "Content-Length: 0" -o PublishResult.json
        publishResult=$(cat PublishResult.json)

        if [[ "$publishResult" == *"error"* ]]; 
        then
            echo "Error while publishing: $publishResult"
            exit 1
        fi
        echo "App published successfully"
 
        exit 0
    fi
    sleep 6
done

echo "Training timeout"
exit 1
