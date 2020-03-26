﻿using System;
using Template10.Extensions;
using Prism.Ioc;
using Template10.Services.File;
using Template10.Services.Serialization;
using Prism;

namespace Template10.Services.Settings
{
    public class RoamingFileSettingsAdapter : ISettingsAdapter
    {
        private readonly IFileService _helper;

        public RoamingFileSettingsAdapter()
          : this(PrismApplicationBase.Current.Container.Resolve<ISerializationService>())
        {
            // empty
        }

        public RoamingFileSettingsAdapter(ISerializationService serializationService)
        {
            _helper = new File.FileService(serializationService);
            SerializationService = serializationService;
        }

        public ISerializationService SerializationService { get; }

        public RoamingFileSettingsAdapter(IFileService fileService)
        {
            _helper = fileService;
        }

        public (bool successful, string result) ReadString(string key)
        {
            return (true, _helper.ReadStringAsync(key, StorageStrategies.Roaming).Result);
        }

        public void WriteString(string key, string value)
        {
            if (!_helper.WriteStringAsync(key, value, StorageStrategies.Roaming).Result)
            {
                throw new Exception();
            }
        }
    }
}
