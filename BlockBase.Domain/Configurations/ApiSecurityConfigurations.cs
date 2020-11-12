using System.ComponentModel.DataAnnotations;

namespace BlockBase.Domain.Configurations
{
    public class ApiSecurityConfigurations
    {
        public bool Use { get; set; }
        public string ApiKey { get; set; }
        public bool ExecuteQuerySkipEndpointAuth { get; set; }
    }
}