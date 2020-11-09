using System;
using System.IO;
using System.Linq;
using Biggy.Data.Json;
using NUnit.Framework;

namespace Tests.Json
{
    [TestFixture()]
    [Category("Json DbCore")]
    public class JsonDbCore_Tests
    {
        private JsonDbCore _db;

        [SetUp]
        public void init()
        {
        }

        [Test()]
        public void Initializes_JsonDb_In_Project_Root_In_Defeault_Folder()
        {
            string projectRoot = Directory.GetParent(@"..\..\").FullName;
            string expectedDirectory = Path.Combine(projectRoot, @"Data\Json\testhost");

            bool directoryExists = Directory.Exists(expectedDirectory);
            if (directoryExists)
            {
                Directory.Delete(expectedDirectory, true);
            }
            _db = new JsonDbCore();
            directoryExists = Directory.Exists(expectedDirectory);

            Assert.True(_db.DbDirectory == expectedDirectory && directoryExists);
        }

        [Test()]
        public void Initializes_JsonDb_With_Project_Root_With_Folder_Option()
        {
            string alternateDbFolder = "SillyTestDbName";
            string projectRoot = Directory.GetParent(@"..\..\").FullName;
            string expectedDirectory = Path.Combine(projectRoot, @"Data\Json\", alternateDbFolder);

            if (Directory.Exists(expectedDirectory))
            {
                Directory.Delete(expectedDirectory, true);
            }
            _db = new JsonDbCore(alternateDbFolder);
            bool directoryExists = Directory.Exists(expectedDirectory);

            Assert.True(_db.DbDirectory == expectedDirectory && directoryExists);
        }

        [Test()]
        public void Initializes_JsonDb_With_Alternate_Directory()
        {
            string alternateDbFolder = "SillyTestDbName";
            string alternateDbDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string targetDirectoryRoot = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string expectedDirectory = Path.Combine(targetDirectoryRoot, alternateDbFolder);

            Directory.Delete(expectedDirectory, true);
            _db = new JsonDbCore(alternateDbDirectory, alternateDbFolder);
            bool directoryExists = Directory.Exists(expectedDirectory);

            Assert.True(_db.DbDirectory == expectedDirectory && directoryExists);
        }
    }
}