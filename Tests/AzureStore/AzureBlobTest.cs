using System;
using System.Collections.Generic;
using System.Linq;
using Biggy.Data.Azure;
using NUnit.Framework;

namespace Tests.AzureStore
{
    [TestFixture]
    public class AzureBlobTest
    {
        private int quantityToAdd;

        //[Test]
        public void Azure_Blob_SaveToAzureExampleData()
        {
            // Given
            var azureCore = new AzureBlobCore("azure_dev");
            var exmapleItmes = this.GetExampleItems();

            // When
            azureCore.SaveAll(exmapleItmes);

            // Then
        }

        //[Test]
        public void Azure_Blob_ReadFromBlob()
        {
            // Given
            var azureCore = new AzureBlobCore("azure_dev");
            var exmapleItmes = this.GetExampleItems();
            azureCore.SaveAll(exmapleItmes);

            // When
            var result = azureCore.GetAll<InstrumentDocuments>();

            // Then
            Assert.AreEqual(this.quantityToAdd, result.Count());
        }

        [Test]
        public void Azure_Blob_CreateStoreFromCore()
        {
            // Given
            var azureBlobCore = new AzureBlobCore("azure_dev");

            // When
            var store = azureBlobCore.CreateStoreFor<InstrumentDocuments>();

            // Then
            Assert.IsTrue(store is AzureStore<InstrumentDocuments>);
        }

        [SetUp]
        public void Initialise()
        {
            this.quantityToAdd = 10;
        }

        private List<InstrumentDocuments> GetExampleItems()
        {
            var items = new List<InstrumentDocuments>(quantityToAdd);

            for (int i = 0; i < this.quantityToAdd; i++)
            {
                items.Add(new InstrumentDocuments
                {
                    Id = "USA #" + i,
                    Category = "String # " + i,
                    Type = "Guitar"
                });
            }

            return items;
        }
    }
}