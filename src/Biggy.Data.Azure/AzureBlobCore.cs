using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using Biggy.Core;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;

namespace Biggy.Data.Azure
{
    public class AzureBlobCore : IAzureDataProvider
    {
        private readonly CloudBlobClient blobClient;

        private readonly string containerName;

        public AzureBlobCore(string connectionStringName)
        {
            var connectionString = ConfigurationManager.ConnectionStrings[connectionStringName].ConnectionString;
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            this.blobClient = storageAccount.CreateCloudBlobClient();
            this.containerName = "biggy";
        }

        public static IDataStore<T> CreateStoreFor<T>(string connectionStringName)
            where T : new()
        {
            return new AzureBlobCore(connectionStringName)
                .CreateStoreFor<T>();
        }

        public IDataStore<T> CreateStoreFor<T>()
            where T : new()
        {
            return new AzureStore<T>(this);
        }

        public IEnumerable<T> GetAll<T>()
        {
            var blob = this.CreateBlob<T>();
            var rawData = blob.DownloadTextAsync().GetAwaiter().GetResult();

            return JsonConvert.DeserializeObject<IEnumerable<T>>(rawData);
        }

        public void SaveAll<T>(IEnumerable<T> items)
        {
            var blob = this.CreateBlob<T>();

            AzureBlobCore.AzureBlobCoreSaveToBlob(items, blob);
        }

        private static void AzureBlobCoreSaveToBlob<T>(IEnumerable<T> items, CloudBlockBlob blob)
        {
            var serializedData = JsonConvert.SerializeObject(items);
            blob.UploadTextAsync(serializedData).GetAwaiter().GetResult();
        }

        private static void SerializerJsonToStream<T>(IEnumerable<T> items, StreamWriter writer)
        {
            var jsonWriter = new JsonTextWriter(writer);
            var serializer = JsonSerializer.CreateDefault();
            serializer.Serialize(jsonWriter, items);

            writer.Flush();
        }

        private static void ResetStream(MemoryStream stream)
        {
            stream.Position = 0;
        }

        private static string GetBlobName<T>()
        {
            var type = typeof(T);
            return type.Name;
        }

        private CloudBlockBlob CreateBlob<T>()
        {
            var biggyContainer = this.blobClient.GetContainerReference(this.containerName);
            var blobName = AzureBlobCore.GetBlobName<T>();

            biggyContainer.CreateIfNotExistsAsync().GetAwaiter().GetResult();

            var blob = biggyContainer.GetBlockBlobReference(blobName);
            return blob;
        }
    }
}