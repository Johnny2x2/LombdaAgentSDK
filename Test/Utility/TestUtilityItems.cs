using LombdaAgentSDK;
using LombdaAgentSDK.Agents;

namespace Test.Utility
{
    internal class TestUtilityItems
    {
        public class Animal
        {
            public string Name { get; set; }
            public int Age { get; set; }
        }

        [Test]
        public void TestJsonFormatSchemaCreationOnClass()
        {
            ModelOutputFormat modelFormat = json_util.CreateJsonSchemaFormatFromType(typeof(Animal), true);
            Assert.That(modelFormat.JsonSchemaFormatName, Is.EqualTo("Animal"));
            Assert.That(modelFormat.JsonSchema, Is.Not.Null);
            Assert.That(modelFormat.JsonSchemaIsStrict, Is.True);
        }

        public struct AnimalStruct
        {
            public string Name { get; set; }
            public int Age { get; set; }
        }

        [Test]
        public void TestJsonFormatSchemaCreationOnStruct()
        {
            ModelOutputFormat modelFormat = json_util.CreateJsonSchemaFormatFromType(typeof(AnimalStruct), true);
            Assert.That(modelFormat.JsonSchemaFormatName, Is.EqualTo("Animal"));
            Assert.That(modelFormat.JsonSchema, Is.Not.Null);
            Assert.That(modelFormat.JsonSchemaIsStrict, Is.True);
        }

        [Test]
        public void TestJsonParser()
        {
            string json = "{\"Name\":\"Dog\",\"Age\":5}";
            Animal animal = json.ParseJson<Animal>();
            Assert.That(animal.Name, Is.EqualTo("Dog"));
            Assert.That(animal.Age, Is.EqualTo(5));
        }
    }
}
