using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Biggy.Core;
using Biggy.Data.Azure;
using Microsoft.WindowsAzure.Storage;
using NUnit.Framework;

namespace Tests.Json
{
    [TestFixture()]
    [Category("Azure Store")]
    public class AzureStoreUsingBlobWithStringKey
    {
        private const string ConnectionStringName = "azure_dev";

        //[SetUp]
        public void Initialise()
        {
            var connectionString = ConfigurationManager.ConnectionStrings[AzureStoreUsingBlobWithStringKey.ConnectionStringName].ConnectionString;
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var containerName = "biggy";
            blobClient.GetContainerReference(containerName).DeleteIfExistsAsync().GetAwaiter().GetResult();
        }

        //[Test()]
        public void AzureStore_Inserts_record_with_string_id()
        {
            // Given
            var instrumentStore = GetAzureStore();
            InstrumentDocuments newInstrument = GetSingleItem();

            // When
            instrumentStore.Add(newInstrument);

            // Then
            var newVersionOfinstrumentStore = GetAzureStore();
            var foundInstrument = newVersionOfinstrumentStore.TryLoadData().FirstOrDefault(i => i.Id == "USA123");

            Assert.IsNotNull(foundInstrument);
        }

        //[Test()]
        public void AzureStore_Inserts_range_of_records_with_string_id()
        {
            // Given
            var instrumentStore = GetAzureStore();
            var quantityToAdd = 10;
            var batch = GetBatch(quantityToAdd);

            // When
            instrumentStore.Add(batch);

            // Then
            var companies = instrumentStore.TryLoadData();
            Assert.IsTrue(companies.Count == quantityToAdd);
        }

        //[Test()]
        public void AzureStore_Updates_record_with_string_id()
        {
            // Given
            var instrumentStore = GetAzureStore();
            var newInstrument = GetSingleItem();
            instrumentStore.Add(newInstrument);

            // When
            string newType = "Banjo";
            var foundInstrument = instrumentStore.TryLoadData().FirstOrDefault(i => i.Id == "USA123");
            foundInstrument.Type = newType;
            instrumentStore.Update(foundInstrument);

            // Then
            // Now go fetch the record again and update:
            Assert.IsTrue(foundInstrument != null && foundInstrument.Type == newType);
        }

        //[Test()]
        public void AzureStore_Updates_range_of_records_with_string_id()
        {
            // Given
            var instrumentStore = GetAzureStore();
            var quantityToAdd = 10;
            var batch = GetBatch(quantityToAdd);
            instrumentStore.Add(batch);

            // When
            var companies = instrumentStore.TryLoadData();
            for (int i = 0; i < quantityToAdd; i++)
            {
                companies.ElementAt(i).Type = "Banjo " + i;
            }
            instrumentStore.Update(companies);

            // Then
            companies = instrumentStore.TryLoadData().Where(c => c.Type.StartsWith("Banjo")).ToList();
            Assert.IsTrue(companies.Count == quantityToAdd);
        }

        //[Test()]
        public void AzureStore_Deletes_record_with_string_id()
        {
            // Given
            var instrumentStore = GetAzureStore();
            var newInstrument = GetSingleItem();
            instrumentStore.Add(newInstrument);

            // When
            var foundInstrument = instrumentStore
                .TryLoadData()
                .First();
            instrumentStore.Delete(foundInstrument);

            // Then
            int remaining = instrumentStore.TryLoadData().Count;
            Assert.AreEqual(0, remaining);
        }

        //[Test()]
        public void AzureStore_Deletes_range_of_records_with_string_id()
        {
            // Given
            var instrumentStore = GetAzureStore();
            var quantityToAdd = 10;
            var batch = GetBatch(quantityToAdd);
            instrumentStore.Add(batch);

            // Re-load from back-end:
            var companies = instrumentStore.TryLoadData();
            int qtyAdded = companies.Count;

            // When
            int qtyToDelete = 5;
            var deleteThese = new List<InstrumentDocuments>();
            for (int i = 0; i < qtyToDelete; i++)
            {
                deleteThese.Add(companies.ElementAt(i));
            }

            // Delete:
            instrumentStore.Delete(deleteThese);
            int remaining = instrumentStore.TryLoadData().Count;

            // Then
            Assert.IsTrue(qtyAdded == quantityToAdd);
            Assert.IsTrue(remaining == qtyAdded - qtyToDelete);
        }

        //[Test()]
        public void AzureStore_Deletes_all_records_with_string_id()
        {
            var instrumentStore = GetAzureStore();
            var myBatch = new List<InstrumentDocuments>();
            int qtyToAdd = 10;
            for (int i = 0; i < qtyToAdd; i++)
            {
                myBatch.Add(new InstrumentDocuments { Id = "USA #" + i, Category = "String # " + i, Type = "Guitar" });
            }
            instrumentStore.Add(myBatch);

            // Re-load from back-end:
            var companies = instrumentStore.TryLoadData();
            int qtyAdded = companies.Count;

            // Delete:
            instrumentStore.DeleteAll();
            int remaining = instrumentStore.TryLoadData().Count;
            Assert.IsTrue(qtyAdded == qtyToAdd && remaining == 0);
        }

        private static InstrumentDocuments GetSingleItem()
        {
            return new InstrumentDocuments
            {
                Id = "USA123",
                Category = "String",
                Type = "Guitar"
            };
        }

        private static IEnumerable<InstrumentDocuments> GetBatch(int quantityToAdd = 10)
        {
            var batch = new List<InstrumentDocuments>(quantityToAdd);
            for (int i = 1; i <= quantityToAdd; i++)
            {
                batch.Add(new InstrumentDocuments
                {
                    Id = "USA #" + i,
                    Category = "String # " + i,
                    Type = "Guitar"
                });
            }

            return batch;
        }

        private static IDataStore<InstrumentDocuments> GetAzureStore()
        {
            return AzureBlobCore.CreateStoreFor<InstrumentDocuments>(AzureStoreUsingBlobWithStringKey.ConnectionStringName);
        }
    }
}