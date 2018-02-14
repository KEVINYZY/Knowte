﻿using Digimezzo.Foundation.Core.Logging;
using Knowte.Plugin.Contracts.Collection.Entities;
using Knowte.Presentation.Contracts.Entities;
using Knowte.Services.Contracts.Collection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Knowte.Services.Collection
{
    public class CollectionService : ICollectionService
    {
        // ProviderName doesn't matter here (it won't appear in the list of available services)
        public string ProviderName => string.Empty;

        private CollectionProviderImporter importer = new CollectionProviderImporter();

        public event CollectionChangedEventHandler CollectionAdded = delegate { };
        public event CollectionChangedEventHandler CollectionEdited = delegate { };
        public event CollectionChangedEventHandler CollectionDeleted = delegate { };
        public event CollectionChangedEventHandler ActiveCollectionChanged = delegate { };

        public CollectionService()
        {
            this.importer.DoImport();
        }

        public async Task<bool> ActivateCollectionAsync(CollectionViewModel collection)
        {
            if (collection == null)
            {
                LogClient.Error($"{nameof(collection)} is null");
                return false;
            }

            if (string.IsNullOrEmpty(collection.Id))
            {
                LogClient.Error($"{nameof(collection.Id)} is null or empty");
                return false;
            }


            bool activateSuccess = false;

            try
            {
                activateSuccess = await this.importer.GetProvider().ActivateCollectionAsync(collection.Id);
            }
            catch (Exception ex)
            {
                LogClient.Error($"Activate failed. Exception: {ex.Message}");
            }

            if (!activateSuccess)
            {
                LogClient.Error($"Activate failed. {nameof(collection.Id)}={collection.Id}");
                return false;
            }

            this.ActiveCollectionChanged(this, new CollectionChangedEventArgs(collection.Id));
            LogClient.Info($"Activate successful. {nameof(collection.Id)}={collection.Id}");

            return true;
        }

        public async Task<ChangeCollectionResult> AddCollectionAsync(string title, bool isActive)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                LogClient.Error($"{nameof(title)} is empty");
                return ChangeCollectionResult.Invalid;
            }

            string existingCollectionId = string.Empty;

            try
            {
                existingCollectionId = await this.importer.GetProvider().GetCollectionIdAsync(title);
            }
            catch (Exception ex)
            {
                LogClient.Error($"Add failed. Exception: {ex.Message}");
            }

            if (!string.IsNullOrEmpty(existingCollectionId))
            {
                LogClient.Error($"There is already a collection with the title '{title}'");
                return ChangeCollectionResult.Duplicate;
            }

            string collectionId = string.Empty;

            try
            {
                collectionId = await this.importer.GetProvider().AddCollectionAsync(title, isActive);
            }
            catch (Exception ex)
            {
                LogClient.Error($"Add failed. Exception: {ex.Message}");
            }

            if (string.IsNullOrEmpty(collectionId))
            {
                LogClient.Error($"Add failed. {nameof(collectionId)} is empty");
                return ChangeCollectionResult.Error;
            }

            this.CollectionAdded(this, new CollectionChangedEventArgs(collectionId));
            LogClient.Info($"Add successful. {nameof(title)}={title}");

            return ChangeCollectionResult.Ok;
        }

        public async Task<bool> DeleteCollectionAsync(CollectionViewModel collection)
        {
            if (collection == null)
            {
                LogClient.Error($"{nameof(collection)} is null");
                return false;
            }

            if (string.IsNullOrEmpty(collection.Id))
            {
                LogClient.Error($"{nameof(collection.Id)} is null or empty");
                return false;
            }

            bool deleteSuccess = false;

            try
            {
                deleteSuccess = await this.importer.GetProvider().DeleteCollectionAsync(collection.Id);
            }
            catch (Exception ex)
            {
                LogClient.Error($"Delete failed. Exception: {ex.Message}");
            }

            if (!deleteSuccess)
            {
                LogClient.Error($"Delete failed. {nameof(collection.Id)}={collection.Id}");
                return false;
            }

            this.CollectionDeleted(this, new CollectionChangedEventArgs(collection.Id));
            LogClient.Info($"Delete successful. {nameof(collection.Id)}={collection.Id}");

            return true;
        }

        public async Task<ChangeCollectionResult> EditCollectionAsync(CollectionViewModel collection, string title)
        {
            if (collection == null)
            {
                LogClient.Error($"{nameof(collection)} is null");
                return ChangeCollectionResult.Invalid;
            }

            if (collection == null)
            {
                LogClient.Error($"{nameof(collection)} is null");
                return ChangeCollectionResult.Invalid;
            }

            if (string.IsNullOrEmpty(collection.Id))
            {
                LogClient.Error($"{nameof(collection.Id)} is null or empty");
                return ChangeCollectionResult.Invalid;
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                LogClient.Error($"{nameof(title)} is empty");
                return ChangeCollectionResult.Invalid;
            }

            string existingCollectionId = string.Empty;

            try
            {
                existingCollectionId = await this.importer.GetProvider().GetCollectionIdAsync(title);
            }
            catch (Exception ex)
            {
                LogClient.Error($"Edit failed. Exception: {ex.Message}");
            }

            if (!string.IsNullOrEmpty(existingCollectionId))
            {
                LogClient.Error($"Collection with {nameof(title)}={title} already exists");
                return ChangeCollectionResult.Duplicate;
            }

            bool editSuccess = false;
            try
            {
                editSuccess = await this.importer.GetProvider().EditCollectionAsync(collection.Id, title);
            }
            catch (Exception ex)
            {
                LogClient.Error($"Edit failed. Exception: {ex.Message}");
            }

            if (!editSuccess)
            {
                LogClient.Error($"Edit failed. {nameof(collection.Id)}={collection.Id}, {nameof(title)}={title}");
                return ChangeCollectionResult.Error;
            }

            this.CollectionEdited(this, new CollectionChangedEventArgs(collection.Id));
            LogClient.Info($"Edit success. {nameof(collection.Id)}={collection.Id}, {nameof(title)}={title}");

            return ChangeCollectionResult.Ok;
        }

        public async Task<List<CollectionViewModel>> GetCollectionsAsync()
        {
            List<ICollection> collections = null;

            try
            {
                collections = await this.importer.GetProvider().GetCollectionsAsync();
            }
            catch (Exception ex)
            {
                LogClient.Error($"Get failed. Exception: {ex.Message}");
            }

            if (collections == null || collections.Count.Equals(0))
            {
                LogClient.Error($"{nameof(collections)} is null or empty");
                return new List<CollectionViewModel>();
            }

            var collectionViewModels = new List<CollectionViewModel>();

            foreach (Data.Contracts.Entities.Collection collection in collections)
            {
                collectionViewModels.Add(new CollectionViewModel(collection.Id, collection.Title, collection.IsActive));
            }

            return collectionViewModels.OrderBy(c => c.Title).ToList();
        }
    }
}
