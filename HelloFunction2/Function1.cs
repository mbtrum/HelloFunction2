using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
// Additional Namespaces
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using HelloFunction2.Model;
using System.Collections.Generic;

namespace HelloFunction2
{
    public static class Function1
    {
        private static string sqlconnection;

        [FunctionName("Function1")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log, ExecutionContext context)
        {
            #region

            log.LogInformation("C# HTTP trigger function processed a request.");

            // Initialize
            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
          
            sqlconnection = config.GetConnectionString("AzureSqlConnection");

            // Handle a GET and POST http request            
            if(req.Method == HttpMethods.Get)            
            {
                log.LogInformation("GET http request received.");

                // Get list of persons
                List<Person> persons = GetPersons();
                
                return new JsonResult(persons);
            }
            else if(req.Method == HttpMethods.Post)
            {
                log.LogInformation("POST http request received.");

                // To-do: json validation

                // Deserialze json and store name in Db.
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                Person person = JsonConvert.DeserializeObject<Person>(requestBody);

                // To-do: person obj validation (e.g. Name)

                InsertIntoDb(person);

                return new OkObjectResult("Json data received successfully.");
            }
            else
            {
                return new BadRequestObjectResult("Invalid request.");
            }

            //string name = req.Query["name"];
            //string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            //dynamic data = JsonConvert.DeserializeObject(requestBody);
            //name = name ?? data?.name;

            //string responseMessage = string.IsNullOrEmpty(name)
            //    ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
            //    : $"Hello, {name}. This HTTP triggered function executed successfully.";

            //InsertIntoDb(name);

            //return new OkObjectResult(responseMessage);

            #endregion
        }

        private static void InsertIntoDb(Person person)
        {
            #region
            
            string sql = "insert into dbo.TestTable ([Description]) values (@name)";

            using (SqlConnection connection = new SqlConnection(sqlconnection))
            {
                SqlCommand command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@name", person.Name);

                connection.Open();
                command.ExecuteNonQuery();
            }

            #endregion
        }
       
        private static List<Person> GetPersons()
        {
            #region

            List<Person> persons = new List<Person>();

            string sql = "select [Id], [Description] from dbo.TestTable";

            using (SqlConnection connection = new SqlConnection(sqlconnection))
            {
                SqlCommand command = new SqlCommand(sql, connection);

                connection.Open();

                SqlDataReader reader = command.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        persons.Add(new Person() { 
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1)
                        });
                       
                        //Person person = new Person();
                        //person.Name = reader.GetString(0);
                        //persons.Add(person);
                        
                    }
                }
                reader.Close();
            }

            return persons;

            #endregion
        }
    }
}
