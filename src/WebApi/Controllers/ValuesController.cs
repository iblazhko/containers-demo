using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using static System.Diagnostics.Trace;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        private readonly IMongoDatabase db;

        public ValuesController()
        {
            var mongoClientSettings = new MongoClientSettings
            {
                Server = new MongoServerAddress(MongoDbConfiguration.ServerAddress, MongoDbConfiguration.ServerPort),
                MaxConnectionIdleTime = TimeSpan.FromMinutes(1)
            };

            var mongoClient = new MongoClient(mongoClientSettings);
            db = mongoClient.GetDatabase(MongoDbConfiguration.DatabaseName);

            var c = db.GetCollection<IdValuePairType>("values");
            if (c == null)
            {
                db.CreateCollection("values");
            }
        }

        // GET api/values
        [HttpGet]
        public IEnumerable<IdValuePairType> Get()
        {
            TraceInformation("GET All");

            var c = db.GetCollection<IdValuePairType>("values");
            return c.Find(Builders<IdValuePairType>.Filter.Empty).ToList();
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public IdValuePairType Get(string id)
        {
            TraceInformation($"GET {id}");
            var c = db.GetCollection<IdValuePairType>("values");
            return c.Find(Builders<IdValuePairType>.Filter.Eq(x => x.id, id)).FirstOrDefault();
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]ValueType valueEnvelope)
        {
            var id = Guid.NewGuid().ToString();
            var v = valueEnvelope?.value;
            TraceInformation($"POST {id} {v}");
            var c = db.GetCollection<IdValuePairType>("values");
            var d = new IdValuePairType(id, v);
            c.InsertOne(d);
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(string id, [FromBody]ValueType valueEnvelope)
        {
            var v = valueEnvelope?.value;
            TraceInformation($"PUT {id} {v}");
            var c = db.GetCollection<IdValuePairType>("values");
            c.UpdateOne(Builders<IdValuePairType>.Filter.Eq(x => x.id, id), Builders<IdValuePairType>.Update.Set(x => x.value, v));
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(string id)
        {
            TraceInformation($"DELETE {id}");
            var c = db.GetCollection<IdValuePairType>("values");
            c.DeleteOne(Builders<IdValuePairType>.Filter.Eq(x => x.id, id));
        }

        public class ValueType
        {
            public string value { get; }

            public ValueType(string value)
            {
                this.value = value;
            }
        }

        public class IdValuePairType
        {
            public string id { get; }
            public string value { get;}

            public IdValuePairType(string id, string value)
            {
                this.id = id;
                this.value = value;
            }
        }
    }
}
