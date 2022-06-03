# CreateServicePrincipal
CreateServicePrincipal

In your own Azure tenant, create an App Registration for this 
.  Create a client secret for that app registration and save it for use (you won't be able to access it again).  

Assign the role Azure Role: Application administrator to your app

Copy the values into these variables.    

            var tenantId = "{tenantId}";
            // Values from app registration
            var clientId = "{clientId}";
            var clientSecret = "{clientSecret}";

Please note that you would never really do this for non-demo code.  Use a Key Vault or the like to store your secrets.

[![.NET](https://github.com/RobertoBorges/Assign-SPN-AcrPull-Demo/actions/workflows/dotnet.yml/badge.svg?branch=main)](https://github.com/RobertoBorges/Assign-SPN-AcrPull-Demo/actions/workflows/dotnet.yml)
