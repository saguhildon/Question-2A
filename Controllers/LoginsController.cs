using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQProducerAPI.Models;
using Microsoft.AspNetCore.WebUtilities;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using RabbitMQ.Client;

namespace RabbitMQProducerAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginsController : ControllerBase
    {
        private readonly LoginContext _context;
        private readonly ILogger _logger;

        public LoginsController(LoginContext context, ILogger<LoginsController> logger)
        {
            _context = context;
            _logger = logger;
        }
       
        private async Task<string> LoginValidate(Login login)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var query = new Dictionary<string, string>()
                    {
                        ["email"] = "eve.holt@reqres.in",
                        ["password"] = "cityslicka",
                        ["task"] = "task"
                    };
                    var uri = QueryHelpers.AddQueryString("https://reqres.in/api/login", query);
                    var result = await httpClient.GetAsync(uri);
                    if (result.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        var content = await result.Content.ReadAsStringAsync();
                        
                    }
                }

                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri("https://reqres.in/api/login");
                client.DefaultRequestHeaders
                      .Accept
                      .Add(new MediaTypeWithQualityHeaderValue("application/json"));//ACCEPT header

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "relativeAddress");
                request.Content = new StringContent("{\"email\":\"eve.holt@reqres.in\",\"password\":\"cityslicka\",\"task\":\"Any Task\"}",
                                                    Encoding.UTF8,
                                                    "application/json");//CONTENT-TYPE header

                var res = client.SendAsync(request)
                      .ContinueWith(responseTask =>
                      {
                          //Console.WriteLine("Response: {0}", responseTask.Result);
                      });
                HttpClient client1 = new HttpClient();
                HttpResponseMessage response = await
                    client1.PostAsJsonAsync("https://reqres.in/api/login",
                                new
                                {
                                    email = login.email,
                                    password = login.password,
                                    task=login.task
                                });
               // Console.WriteLine(response);
                var deserialized = JsonConvert.DeserializeObject<Dictionary<string, string>>(await response.Content.ReadAsStringAsync());
                Console.WriteLine(deserialized);
                Console.WriteLine(deserialized["token"]);                              
                return deserialized["token"].ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError("Could not locate the location" + ex.ToString());
                return null;
            }

        }

        // GET: api/Logins
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Login>>> GetLoginItems()
        {
            return await _context.LoginItems.ToListAsync();
        }

        // GET: api/Logins/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Login>> GetLogin(int id)
        {
            var login = await _context.LoginItems.FindAsync(id);

            if (login == null)
            {
                return NotFound();
            }

            return login;
        }

        // PUT: api/Logins/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutLogin(int id, Login login)
        {
            if (id != login.ID)
            {
                return BadRequest();
            }

            _context.Entry(login).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LoginExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Logins
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Login>> PostLogin(Login login)
        {
            try
            {
                var token = await LoginValidate(login);
                Console.WriteLine("token to produce:" + token);
                var factory = new ConnectionFactory()
                {
                    //HostName = "localhost",
                    //Port = 31672
                    HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST"),
                    Port = Convert.ToInt32(Environment.GetEnvironmentVariable("RABBITMQ_PORT"))

                };

                Console.WriteLine(factory.HostName + ":" + factory.Port);
                using (var connection = factory.CreateConnection())
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare(queue: "TaskQueue",
                                         durable: false,
                                         exclusive: false,
                                         autoDelete: false,
                                         arguments: null);

                    string message = token;
                    var body = Encoding.UTF8.GetBytes(message);

                    channel.BasicPublish(exchange: "",
                                         routingKey: "TaskQueue",
                                         basicProperties: null,
                                         body: body);
                }
                await _context.SaveChangesAsync();

                return CreatedAtAction("GetLogin", new { id = login.ID }, login);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return null;
            }
           

           // return CreatedAtAction("GetLogin", new { id = login.ID }, login);
        }

        // DELETE: api/Logins/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLogin(int id)
        {
            var login = await _context.LoginItems.FindAsync(id);
            if (login == null)
            {
                return NotFound();
            }

            _context.LoginItems.Remove(login);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool LoginExists(int id)
        {
            return _context.LoginItems.Any(e => e.ID == id);
        }
    }
}
