using System;
using System.IO;
using DG.Tweening;
using log4net;
using log4net.Core;
using Loom.Client;
using Loom.ZombieBattleground.BackendCommunication;
using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Data;
using Loom.ZombieBattleground.Gameplay;
using Newtonsoft.Json;
using UnityEngine;
using Logger = log4net.Repository.Hierarchy.Logger;

namespace Loom.ZombieBattleground
{
    public class GameClient : ServiceLocatorBase
    {
        private static readonly ILog Log = Logging.GetLog(nameof(GameClient));

        public event Action ServicesInitialized;

        public bool UpdateServices { get; set; } = true;

        private static readonly object Sync = new object();

        private static GameClient _instance;

        /// <summary>
        ///     Initializes a new instance of the <see cref="GameClient" /> class.
        /// </summary>
        internal GameClient()
        {
            ConfigureLoggers();
            Log.Info("Starting game, version " + BuildMetaInfo.Instance.FullVersionName);

            DOTween.KillAll();
            LoadObjectsManager loadObjectsManager = new LoadObjectsManager();
            loadObjectsManager.LoadAssetBundleFromFile(Constants.AssetBundleMain);

            BackendEndpoint backendEndpoint = GetDefaultBackendEndpoint();

            Func<Contract, IContractCallProxy> contractCallProxyFactory =
                contract => new ThreadedContractCallProxyWrapper(new TimeMetricsContractCallProxy(contract, true, true));

            AddService<IApplicationSettingsManager>(new ApplicationSettingsManager());
            AddService<ILoadObjectsManager>(loadObjectsManager);
            AddService<ITimerManager>(new TimerManager());
            AddService<IInputManager>(new InputManager());
            AddService<ILocalizationManager>(new LocalizationManager());
            AddService<IScenesManager>(new ScenesManager());
            AddService<IAppStateManager>(new AppStateManager());
            AddService<ICameraManager>(new CameraManager());
            AddService<IPlayerManager>(new PlayerManager());
            AddService<ISoundManager>(new SoundManager());
            AddService<INavigationManager>(new NavigationManager());
            AddService<IGameplayManager>(new GameplayManager());
            AddService<IOverlordExperienceManager>(new OverlordExperienceManager());
            AddService<ITutorialManager>(new TutorialManager());
            AddService<IMatchManager>(new MatchManager());
            AddService<IUIManager>(new UIManager());
            AddService<IDataManager>(new DataManager(GetConfigData()));
            AddService<BackendFacade>(
                new BackendFacade(
                    backendEndpoint,
                    contractCallProxyFactory,
                    Logging.GetLog(nameof(BackendFacade)),
                    Logging.GetLog(nameof(BackendFacade) + "Rpc")
                ));
            AddService<ActionCollectorUploader>(new ActionCollectorUploader());
            AddService<BackendDataControlMediator>(new BackendDataControlMediator());
            AddService<IFacebookManager>(new FacebookManager());
            AddService<IAnalyticsManager>(new AnalyticsManager());
            AddService<IPvPManager>(new PvPManager());
            AddService<IQueueManager>(new QueueManager());
            AddService<DebugCommandsManager>(new DebugCommandsManager());
            AddService<PushNotificationManager>(new PushNotificationManager());
            AddService<FiatBackendManager>(new FiatBackendManager());
            AddService<FiatPlasmaManager>(new FiatPlasmaManager());
            AddService<OpenPackPlasmaManager>(new OpenPackPlasmaManager());
            AddService<IInAppPurchaseManager>(new InAppPurchaseManager());
            AddService<TutorialRewardManager>(new TutorialRewardManager());
        }

        public override void InitServices() {
            base.InitServices();

            ServicesInitialized?.Invoke();
        }

        public override void Update()
        {
            if (!UpdateServices)
                return;

            base.Update();
        }

        public static BackendEndpoint GetDefaultBackendEndpoint()
        {
            ConfigData configData = GetConfigData();
            if (configData.Backend != null)
            {
                return configData.Backend;
            }

#if (UNITY_EDITOR || USE_LOCAL_BACKEND) && !USE_PRODUCTION_BACKEND && !USE_STAGING_BACKEND && !USE_BRANCH_TESTING_BACKEND && !USE_REBALANCE_BACKEND
            const BackendPurpose backend = BackendPurpose.Local;
#elif USE_PRODUCTION_BACKEND
            const BackendPurpose backend = BackendPurpose.Production;
#elif USE_BRANCH_TESTING_BACKEND
            const BackendPurpose backend = BackendPurpose.BranchTesting;
#elif USE_REBALANCE_BACKEND
            const BackendPurpose backend = BackendPurpose.Rebalance;
#else
            const BackendPurpose backend = BackendPurpose.Staging;
#endif

            BackendEndpoint backendEndpoint = BackendEndpointsContainer.Endpoints[backend];
            return backendEndpoint;
        }

        public static GameClient Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (Sync)
                    {
                        _instance = new GameClient();
                    }
                }

                return _instance;
            }
        }

        public static T Get<T>()
        {
            return Instance.GetService<T>();
        }

        public static void ClearInstance()
        {
            _instance = null;
        }

        private void ConfigureLoggers()
        {
#if UNITY_EDITOR && !FORCE_ENABLE_ALL_LOGS
            // Disable non-essential logs in Editor
            Logger backendFacadeRpc = Logging.GetLogger(nameof(BackendFacade) + "Rpc");
            backendFacadeRpc.Level = Level.Warn;

            Logger timeMetricsContractCallProxy = Logging.GetLogger(nameof(TimeMetricsContractCallProxy));
            timeMetricsContractCallProxy.Level = Level.Warn;
#endif
        }

        private static ConfigData GetConfigData()
        {
            string configDataFilePath = Path.Combine(Application.persistentDataPath, Constants.LocalConfigDataFileName);
            if (File.Exists(configDataFilePath))
            {
                return JsonConvert.DeserializeObject<ConfigData>(File.ReadAllText(configDataFilePath));
            }
#if UNITY_EDITOR
            configDataFilePath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, Constants.LocalConfigDataFileName);
            if (File.Exists(configDataFilePath))
            {
                return JsonConvert.DeserializeObject<ConfigData>(File.ReadAllText(configDataFilePath));
            }
#endif

            return new ConfigData();
        }
    }
}
