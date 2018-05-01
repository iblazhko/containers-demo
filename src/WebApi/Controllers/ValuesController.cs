using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using static System.Diagnostics.Trace;

namespace WebApi.Controllers
{
    using System.Linq;

    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        private static readonly Dictionary<string, string> repository = new Dictionary<string, string>();

        // GET api/values
        [HttpGet]
        public IdValuePairType[] Get()
        {
            TraceInformation("GET All");
            
            return repository
                .Select(kvp => new IdValuePairType(kvp.Key, kvp.Value))
                .ToArray();
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public IdValuePairType Get(string id)
        {
            TraceInformation($"GET {id}");

            return repository.ContainsKey(id) ? new IdValuePairType(id, repository[id]) : null;
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]ValueType valueEnvelope)
        {
            var id = Guid.NewGuid().ToString();
            TraceInformation($"POST {id}");
            repository.Add(id, valueEnvelope.value);
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(string id, [FromBody]ValueType valueEnvelope)
        {
            TraceInformation($"PUT {id}");
            if (repository.ContainsKey(id))
                repository[id] = valueEnvelope.value;
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(string id)
        {
            TraceInformation($"DELETE {id}");
            if (repository.ContainsKey(id))
                repository.Remove(id);
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
