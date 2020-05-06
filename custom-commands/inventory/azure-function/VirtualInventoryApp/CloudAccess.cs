using System;
using System.Collections.Generic;
using System.Text;

namespace VirtualInventoryApp
{
    class CloudAccess
    {
        //find the connection string under
        //Storage Account -> Settings -> Access Keys
        //Either Connection String works (each one uses a different key)
        public static string CONNECTION_STRING = "DefaultEndpointsProtocol=https;AccountName=landingdemowidget;AccountKey=[AccountKeyGoesHere]==;EndpointSuffix=core.windows.net"; 
    }
}
