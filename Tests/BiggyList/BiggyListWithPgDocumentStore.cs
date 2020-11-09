using System;
using System.Collections.Generic;
using System.Linq;
using Biggy.Core;
using Biggy.Data.Postgres;
using NUnit.Framework;

// Docker command to run Postgres

namespace Tests {
  [TestFixture()]
  [Category("Biggy List with PG Document Store")]
  public class BiggyListWithPgDocumentStore {
    private PgDbCore _db;
    private IDataStore<PropertyDocument> _PropertyDocumentStore;

    [SetUp]
    public void init() {
      _db = new PgDbCore("biggy_test");
      _db.TryDropTable("propertydocuments");
      _PropertyDocumentStore = _db.CreateDocumentStoreFor<PropertyDocument>();
    }

    [Test()]
    public void Table_Created_Created_for_Pg_Store_With_Biggylist() {
      Assert.True(_db.TableExists("propertydocuments"));
    }

    [Test()]
    public void BiggyList_Initializes_From_Empty_Table() {
      var PropertyDocumentStore = _db.CreateRelationalStoreFor<PropertyDocument>();
      var PropertyDocumentList = new BiggyList<PropertyDocument>(_PropertyDocumentStore);
      Assert.True(PropertyDocumentList.Store != null && PropertyDocumentList.Count == 0);
    }

    [Test()]
    public void Biggylist_Adds_Single_Item_To_Pg_Store() {
      var PropertyDocumentList = new BiggyList<PropertyDocument>(_PropertyDocumentStore);
      int initialCount = PropertyDocumentList.Count;

      var newPropertyDocument = new PropertyDocument { Name = "Watergate Apartments", Address = "2639 I St NW, Washington, D.C. 20037" };
      PropertyDocumentList.Add(newPropertyDocument);

      // Reload from the store:
      PropertyDocumentList = new BiggyList<PropertyDocument>(_PropertyDocumentStore);

      var addedItems = PropertyDocumentList.FirstOrDefault(p => p.Name.Contains("Watergate"));
      Assert.IsTrue(initialCount == 0 && PropertyDocumentList.Count == 1 && addedItems.Id == 1);
    }

    [Test()]
    public void Biggylist_Adds_Range_Of_Items_To_Pg_Store() {
      var PropertyDocumentList = new BiggyList<PropertyDocument>(_PropertyDocumentStore);
      int initialCount = PropertyDocumentList.Count;

      var myBatch = new List<PropertyDocument>();
      int qtyToAdd = 10;
      for (int i = 1; i <= qtyToAdd; i++) {
        var newPropertyDocument = new PropertyDocument { Name = "New Apartment " + i, Address = "Some Street in a Lonely Town" };
        myBatch.Add(newPropertyDocument);
      }
      PropertyDocumentList.Add(myBatch);

      // Reload from the store:
      PropertyDocumentList = new BiggyList<PropertyDocument>(_PropertyDocumentStore);

      var addedItems = PropertyDocumentList.Where(p => p.Id > 0);
      Assert.IsTrue(initialCount == 0 && addedItems.Count() > 0);
    }

    [Test()]
    public void Biggylist_Updates_Single_Item_In_Pg_Store() {
      var PropertyDocumentList = new BiggyList<PropertyDocument>(_PropertyDocumentStore);
      int initialCount = PropertyDocumentList.Count;

      var newPropertyDocument = new PropertyDocument { Name = "John's Luxury Apartments", Address = "2639 I St NW, Washington, D.C. 20037" };
      PropertyDocumentList.Add(newPropertyDocument);

      int addedItemId = newPropertyDocument.Id;

      // Just to be sure, reload from backing store and check what was added:
      PropertyDocumentList = new BiggyList<PropertyDocument>(_PropertyDocumentStore);
      var addedPropertyDocument = PropertyDocumentList.FirstOrDefault(p => p.Id == addedItemId);
      bool isAddedProperly = addedPropertyDocument.Name.Contains("John's Luxury");

      // Now Update:
      string newName = "John's Low-Rent Apartments";
      addedPropertyDocument.Name = newName;
      PropertyDocumentList.Update(addedPropertyDocument);

      // Go fetch again:
      PropertyDocumentList = new BiggyList<PropertyDocument>(_PropertyDocumentStore);
      addedPropertyDocument = PropertyDocumentList.FirstOrDefault(p => p.Id == addedItemId);
      bool isUpdatedProperly = addedPropertyDocument.Name == newName;

      Assert.IsTrue(isAddedProperly && isUpdatedProperly);
    }

    [Test()]
    public void Biggylist_Updates_Range_Of_Items_In_Pg_Store() {
      var PropertyDocumentList = new BiggyList<PropertyDocument>(_PropertyDocumentStore);
      int initialCount = PropertyDocumentList.Count;

      var myBatch = new List<PropertyDocument>();
      int qtyToAdd = 10;
      for (int i = 1; i <= qtyToAdd; i++) {
        myBatch.Add(new PropertyDocument { Name = "John's Luxury Townhomes" + i, Address = i + " PropertyDocument Parkway, Portland, OR 97204" });
      }
      PropertyDocumentList.Add(myBatch);

      // Just to be sure, reload from backing store and check what was added:
      PropertyDocumentList = new BiggyList<PropertyDocument>(_PropertyDocumentStore);
      int addedCount = PropertyDocumentList.Count;

      // Update each item:
      for (int i = 0; i < qtyToAdd; i++) {
        PropertyDocumentList.ElementAt(i).Name = "John's Low-Rent Brick Homes " + i;
      }
      PropertyDocumentList.Update(PropertyDocumentList.ToList());

      // Reload, and check updated names:
      PropertyDocumentList = new BiggyList<PropertyDocument>(_PropertyDocumentStore);
      var updatedItems = PropertyDocumentList.Where(p => p.Name.Contains("Low-Rent Brick"));
      Assert.IsTrue(updatedItems.Count() == qtyToAdd);
    }

    [Test()]
    public void Biggylist_Removes_Single_Item_From_Pg_Store() {
      var PropertyDocumentList = new BiggyList<PropertyDocument>(_PropertyDocumentStore);
      int initialCount = PropertyDocumentList.Count;

      var newPropertyDocument = new PropertyDocument { Name = "John's High-End Apartments", Address = "16 PropertyDocument Parkway, Portland, OR 97204" };
      PropertyDocumentList.Add(newPropertyDocument);

      int addedItemId = newPropertyDocument.Id;

      // Just to be sure, reload from backing store and check what was added:
      PropertyDocumentList = new BiggyList<PropertyDocument>(_PropertyDocumentStore);
      var addedPropertyDocument = PropertyDocumentList.FirstOrDefault(p => p.Id == addedItemId);
      bool isAddedProperly = addedPropertyDocument.Name.Contains("High-End");

      int qtyAdded = PropertyDocumentList.Count;

      // Delete:
      var foundPropertyDocument = PropertyDocumentList.FirstOrDefault();
      PropertyDocumentList.Remove(foundPropertyDocument);

      PropertyDocumentList = new BiggyList<PropertyDocument>(_PropertyDocumentStore);
      int remaining = PropertyDocumentList.Count;
      Assert.IsTrue(isAddedProperly && qtyAdded == 1 && remaining == 0);
    }

    [Test()]
    public void Biggylist_Removes_Range_Of_Items_From_Pg_Store() {
      var PropertyDocumentList = new BiggyList<PropertyDocument>(_PropertyDocumentStore);
      int initialCount = PropertyDocumentList.Count;

      var myBatch = new List<PropertyDocument>();
      int qtyToAdd = 10;
      for (int i = 0; i < qtyToAdd; i++) {
        myBatch.Add(new PropertyDocument { Name = "Boardwalk " + i, Address = i + " PropertyDocument Parkway, Portland, OR 97204" });
      }
      PropertyDocumentList.Add(myBatch);

      // Re-load from back-end:
      PropertyDocumentList = new BiggyList<PropertyDocument>(_PropertyDocumentStore);
      int qtyAdded = PropertyDocumentList.Count;

      // Select 5 for deletion:
      int qtyToDelete = 5;
      var deleteThese = PropertyDocumentList.Where(c => c.Id <= qtyToDelete);
      PropertyDocumentList.Remove(deleteThese);

      // Delete:
      PropertyDocumentList = new BiggyList<PropertyDocument>(_PropertyDocumentStore);
      int remaining = PropertyDocumentList.Count;
      Assert.IsTrue(qtyAdded == qtyToAdd && remaining == qtyAdded - qtyToDelete);
    }

    [Test()]
    public void Biggylist_Removes_All_Items_From_Pg_Store() {
      var PropertyDocumentList = new BiggyList<PropertyDocument>(_PropertyDocumentStore);
      int initialCount = PropertyDocumentList.Count;

      var myBatch = new List<PropertyDocument>();
      int qtyToAdd = 10;
      for (int i = 0; i < qtyToAdd; i++) {
        myBatch.Add(new PropertyDocument { Name = "Marvin Gardens " + i, Address = i + " PropertyDocument Parkway, Portland, OR 97204" });
      }
      PropertyDocumentList.Add(myBatch);

      // Re-load from back-end:
      PropertyDocumentList = new BiggyList<PropertyDocument>(_PropertyDocumentStore);
      int qtyAdded = PropertyDocumentList.Count;

      PropertyDocumentList.Clear();

      // Delete:
      PropertyDocumentList = new BiggyList<PropertyDocument>(_PropertyDocumentStore);
      int remaining = PropertyDocumentList.Count;
      Assert.IsTrue(qtyAdded == qtyToAdd && remaining == 0);
    }
  }
}