using System;
using Microsoft.Graph;
using Azure.Identity;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Identity.Client;

namespace CreateServicePrinciple
{

    class Program
    {

        static async Task Main(string[] args)
        {


            // The client credentials flow requires that you request the
            // /.default scope, and preconfigure your permissions on the
            // app registration in Azure. An administrator must grant consent
            // to those permissions beforehand.
            //The Client must be part of the Azure Role: Application administrator
            var scopesForAzureAD = new[] { "https://graph.microsoft.com/.default" };
            var scopesForAzure = new[] { " https://management.azure.com//.default" };

            // Multi-tenant apps can use "common",
            // single-tenant apps must use the tenant ID from the Azure portal
            var tenantId = "<AAD tenant id>";

            // Values from app registration
            var clientId = "<AAD App client id";
            var clientSecret = "<AAD App client secret>";

            //Information about the ACR
            string acrpullRole = "7f951dda-4ed3-4680-a7ca-43fe172d538d";    //This is the ID for AcrPull, more info at: https://docs.microsoft.com/en-us/azure/role-based-access-control/built-in-roles
            string subscriptionID = "<Subscription ID where the ACR is hosted>";
            string acrName = "<ACR name>";                                  //Name of your Azure Container registry
            string resourceGroupName = "<Resource groupe where the ACR is hosted>";

            // using Azure.Identity;
            var options = new TokenCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            };

            // https://docs.microsoft.com/dotnet/api/azure.identity.clientsecretcredential
            var clientSecretCredential = new ClientSecretCredential(
                tenantId, clientId, clientSecret, options);

            var graphClient = new GraphServiceClient(clientSecretCredential, scopesForAzureAD);

            Random seed = new Random();
            String displayName = "KTA" + seed.Next().ToString();

            Console.WriteLine("New displayName = " + displayName);

            Application application = new Application
            {
                DisplayName = displayName //"KTA" + seed.Next().ToString()
            };
            Application app = await graphClient.Applications
                .Request()
                .AddAsync(application);

            ServicePrincipal sp = null;
            if (app.AppId.Length > 0)
            {
                ServicePrincipal servicePrincipal = new ServicePrincipal
                {
                    AppId = app.AppId
                };

                sp = await graphClient.ServicePrincipals
                    .Request()
                    .AddAsync(servicePrincipal);


                var passwordCredential = new PasswordCredential
                {
                    DisplayName = "pa$$w0rd"
                };

                await graphClient.ServicePrincipals[sp.Id]
                        .AddPassword(passwordCredential)
                       .Request()
                       .PostAsync();

            }

            // TODO: Assign AcrPull role of an ACR to this new Service Principal

            // Request a specific token to manage azure resources
            var credential = ConfidentialClientApplicationBuilder
                            .Create(clientId)
                            .WithClientSecret(clientSecret)
                            .WithAuthority(AzureAuthorityHosts.AzurePublicCloud + tenantId)
                            .Build();

            var accessToken = credential.AcquireTokenForClient(scopesForAzure).ExecuteAsync().Result.AccessToken;

            //URL to assign the role acrPull 
            string url = $"https://management.azure.com/subscriptions/{subscriptionID}" +
                         $"/resourceGroups/{resourceGroupName}" +
                         $"/providers/Microsoft.ContainerRegistry/registries/{acrName}" +
                         $"/providers/Microsoft.Authorization/roleAssignments/{sp.Id}?api-version=2020-04-01-preview"; // this ID should be unique for the ACR, so we are using the SPID

            // Build  Json body with the new role assigment
            var role = new RolePropertiesInfo(new RoleDefinitionInfo());
            role.properties.RoleDefinitionId = acrpullRole;
            role.properties.PrincipalId = sp.Id;
            role.properties.subscription = subscriptionID;
            string parameter = JsonSerializer.Serialize(role);
            string result = "";

            using (var client = new HttpClient())
            {
                using (var content = new StringContent(parameter))
                {
                    content.Headers.Remove("Content-Type");
                    content.Headers.Add("Content-Type", "application/json; charset=utf-8");

                    using (var request = new HttpRequestMessage(HttpMethod.Put, url))
                    {

                        request.Headers.Add("Authorization", "Bearer " + accessToken);
                        request.Content = content;

                        using (HttpResponseMessage resp = await client.SendAsync(request))
                        {
                            resp.EnsureSuccessStatusCode();

                            result = await request.Content.ReadAsStringAsync();
                        }
                    }
                }
            }

            Console.WriteLine($"New AppID = {app.AppId} \n role assigned: {result}");
            Console.ReadLine();
        }


    }

    class RolePropertiesInfo
    {
        public RoleDefinitionInfo properties { get; set; }

        public RolePropertiesInfo(RoleDefinitionInfo properties)
        {
            this.properties = properties;
        }
    }
    class RoleDefinitionInfo
    {
        private string _roleDefinitionId = "7f951dda-4ed3-4680-a7ca-43fe172d538d";
        private string _subscription;
        public string RoleDefinitionId
        {
            get
            {
                return $"/subscriptions/{_subscription}/providers/Microsoft.Authorization/roleDefinitions/{_roleDefinitionId}";
            }
            set
            {
                _roleDefinitionId = value;
            }
        }
        public string PrincipalId { get; set; }

        public string PrincipalType
        {
            get
            {
                return "ServicePrincipal";
            }
        }

        public string subscription
        {
            set
            {
                _subscription = value;
            }
        }
    }
}
