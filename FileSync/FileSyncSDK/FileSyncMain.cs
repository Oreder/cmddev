﻿using System;
using System.Collections.Generic;
using FileSyncSDK.Interfaces;
using FileSyncSDK.Implementations;
using FileSyncSDK.Enums;
using FileSyncSDK.Exceptions;
using System.IO;
using System.Linq;

namespace FileSyncSDK
{
    public class FileSyncMain : IMain
    {
        /// <summary>
        /// Конструктор основного класса программы FileSync.
        /// </summary>
        /// <param name="localSettingsPath">Значение параметра LocalSettingsPath.</param>
        /// <param name="progressView">Значение параметра ProgessView, может быть null.</param>
        /// <exception cref="ArgumentNullException">localSettingsPath null.</exception>
        /// <exception cref="SettingsDataCorruptedException">Данные в локальном файле настроек не соответствуют ожидаемому формату.</exception>
        public FileSyncMain(string localSettingsPath, IProgress<IProgressData> progressView)
        {
            LocalSettingsPath = localSettingsPath;
            ProgressView = progressView;
        }

        public string LocalSettingsPath
        {
            get
            {
                return localSettings.FilePath;
            }

            set
            {
                if (localSettings == null)
                    localSettings = new Settings(value, SettingsFileType.Local);
                else
                    localSettings.FilePath = value;

                cloudService = localSettings.CloudService;
            }
        }

        public string UserLogin
        {
            get
            {
                return cloudService.UserLogin;
            }

            set
            {
                cloudService.UserLogin = value;
            }
        }

        public string UserPassword
        {
            get
            {
                return cloudService.UserPassword;
            }

            set
            {
                cloudService.UserPassword = value;
            }

        }

        public string ServiceName
        {
            get
            {
                return cloudService.ServiceName;
            }

            set
            {
                cloudService.ServiceName = value;
            }
        }

        public string ServiceFolderPath
        {
            get
            {
                return cloudService.ServiceFolderPath;
            }

            set
            {
                cloudService.ServiceFolderPath = value;
            }
        }

        public IReadOnlyList<IGroupData> GlobalGroups
        {
            get
            {
                return globalSettings?.Groups.ToList();
            }
        }

        public IReadOnlyList<IGroupData> LocalGroups
        {
            get
            {
                return localSettings?.Groups.ToList();
            }
        }

        public IProgress<IProgressData> ProgressView
        {
            get
            {
                return progress;
            }

            set
            {
                progress = value;
            }
        }

        private ISettings localSettings = null;
        private ISettings globalSettings = null;
        private ICloudService cloudService = null;
        private IProgress<IProgressData> progress = null;

        public void DeleteGroup(string name, bool local, bool global)
        {
            if (local)
            {
                IGroup group = localSettings.Groups.SingleOrDefault(g => g.Name == name);
                if (group != null)
                    localSettings.Groups.Remove(group);
            }

            if (global)
            {
                using (ISession session = cloudService.OpenSession(progress))
                {
                    globalSettings = session.GlobalSettings;
                    IGroup group = globalSettings.Groups.SingleOrDefault(g => g.Name == name);
                    if (group != null)
                        session.DeleteGroup(group);
                }
            }
        }

        public void NewGroup(string name, string[] files, string[] folders)
        {
            if (localSettings.Groups.Any(g => g.Name == name) || globalSettings.Groups.Any(g => g.Name == name))
                throw new ArgumentException("Group with that name already exists.");
            IGroup group = new Group(name, files, folders);
            localSettings.Groups.Add(group);
        }

        public void NewGroup(string globalGroupName)
        {
            if (!globalSettings.Groups.Any(g => g.Name == globalGroupName))
                throw new ArgumentException("Group with that name does not exists in global groups list.");
            if (localSettings.Groups.Any(g => g.Name == globalGroupName))
                throw new ArgumentException("Group with that name already exists in local groups list.");

            IGroup group = new Group(globalSettings.Groups.SingleOrDefault(g => g.Name == globalGroupName), false);
            localSettings.Groups.Add(group);
        }

        public void GetData()
        {
            using (ISession session = cloudService.OpenSession(progress))
            {
                globalSettings = session.GlobalSettings;
            }
            localSettings.Save();
        }

        public void Syncronize()
        {
            using (ISession session = cloudService.OpenSession(progress))
            {
                session.SyncronizeGroups(localSettings.Groups);
                globalSettings = session.GlobalSettings;
            }
            localSettings.Save();
        }

        public bool CloudLoginSuccess()
        {
            try
            {
                using (ISession session = cloudService.OpenSession())
                {
                    localSettings.Save();
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
