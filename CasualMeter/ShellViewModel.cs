using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CasualMeter.Common.Formatters;
using CasualMeter.Common.TeraDpsApi;
using CasualMeter.Core.Conductors;
using CasualMeter.Core.Conductors.Messages;
using CasualMeter.Core.Helpers;
using CasualMeter.Tracker;
using CasualMeter.ViewModels.Base;
using GalaSoft.MvvmLight.CommandWpf;
using Lunyx.Common.UI.Wpf.Collections;
using NetworkSniffer;
using Tera;
using Tera.Data;
using Tera.Game;
using Tera.Game.Abnormality;
using Tera.Game.Messages;
using Tera.Sniffing;

namespace CasualMeter
{
    public class ShellViewModel : CasualViewModelBase
    {
        private ITeraSniffer _teraSniffer;
        private TeraData _teraData;
        private MessageFactory _messageFactory;
        private EntityTracker _entityTracker;
        private PlayerTracker _playerTracker;
        private UserLogoTracker _userLogoTracker = new UserLogoTracker();
        private AbnormalityTracker _abnormalityTracker;
        private readonly AbnormalityStorage _abnormalityStorage = new AbnormalityStorage();
        private readonly Stopwatch _inactivityTimer = new Stopwatch();

        public ShellViewModel()
        {
            CasualMessenger.Instance.Messenger.Register<PastePlayerStatsMessage>(this, PasteStats);
            CasualMessenger.Instance.Messenger.Register<ResetPlayerStatsMessage>(this, ResetDamageTracker);
            CasualMessenger.Instance.Messenger.Register<ExportStatsMessage>(this, ExportStats);
        }

        #region Properties

        public string ApplicationFullName => $"Casual Meter {SettingsHelper.Instance.Version}";

        public BasicTeraData BasicTeraData
        {
            get { return GetProperty(getDefault: () => SettingsHelper.Instance.BasicTeraData); }
            set { SetProperty(value); }
        }

        public Server Server
        {
            get { return GetProperty<Server>(); }
            set { SetProperty(value); }
        }

        public SynchronizedObservableCollection<DamageTracker> ArchivedDamageTrackers
        {
            get { return GetProperty(getDefault: () => new SynchronizedObservableCollection<DamageTracker>()); }
            set { SetProperty(value); }
        }

        public DamageTracker DamageTracker
        {
            get { return GetProperty<DamageTracker>(); }
            set
            {
                SetProperty(value, onChanged: e =>
                {
                    _inactivityTimer.Restart();
                    PlayerCount = value.StatsByUser.Count;
                });
            }
        }
        #endregion

        public bool IsPinned
        {
            get { return GetProperty(getDefault: () => SettingsHelper.Instance.Settings.IsPinned); }
            set
            {
                SetProperty(value, onChanged: e =>
                {
                    SettingsHelper.Instance.Settings.IsPinned = value;
                    ProcessHelper.Instance.ForceVisibilityRefresh(true);
                });
            }
        }
        public bool OnlyBosses
        {
            get { return GetProperty(getDefault: () => SettingsHelper.Instance.Settings.OnlyBosses); }
            set
            {
                SetProperty(value, onChanged: e =>
                {
                    SettingsHelper.Instance.Settings.OnlyBosses = value;
                    if (DamageTracker != null)
                    {
                        DamageTracker.OnlyBosses = value;
                    }
                });
            }
        }

        public bool DetectBosses
        {
            get { return GetProperty(getDefault: () => SettingsHelper.Instance.Settings.DetectBosses); }
            set
            {
                SetProperty(value, onChanged: e =>
                {
                    SettingsHelper.Instance.Settings.DetectBosses = value;
                    if (_teraData?.NpcDatabase != null)
                    {
                        _teraData.NpcDatabase.DetectBosses = value;
                    }
                });
            }
        }

        public bool IgnoreOneshots
        {
            get { return GetProperty(getDefault: () => SettingsHelper.Instance.Settings.IgnoreOneshots); }
            set
            {
                SetProperty(value, onChanged: e =>
                {
                    SettingsHelper.Instance.Settings.IgnoreOneshots = value;
                    if (DamageTracker != null)
                    {
                        DamageTracker.IgnoreOneshots = value;
                    }
                });
            }
        }

        public bool AutosaveEncounters
        {
            get { return GetProperty(getDefault: () => SettingsHelper.Instance.Settings.AutosaveEncounters); }
            set { SetProperty(value, onChanged: e => SettingsHelper.Instance.Settings.AutosaveEncounters = value); }
        }

        public bool PartyOnly
        {
            get { return GetProperty(getDefault: () => SettingsHelper.Instance.Settings.PartyOnly); }
            set { SetProperty(value, onChanged: e => SettingsHelper.Instance.Settings.PartyOnly = value); }
        }

        public bool SiteExport
        {
            get { return GetProperty(getDefault: () => SettingsHelper.Instance.Settings.SiteExport); }
            set { SetProperty(value, onChanged: e => SettingsHelper.Instance.Settings.SiteExport = value); }
        }

        public bool ExcelExport
        {
            get { return GetProperty(getDefault: () => SettingsHelper.Instance.Settings.ExcelExport); }
            set { SetProperty(value, onChanged: e => SettingsHelper.Instance.Settings.ExcelExport = value); }
        }

        public bool ShowCompactView => UseCompactView || (SettingsHelper.Instance.Settings.ExpandedViewPlayerLimit > 0 
                                                          && PlayerCount > SettingsHelper.Instance.Settings.ExpandedViewPlayerLimit);

        public int PlayerCount
        {
            get { return GetProperty<int>(); }
            // ReSharper disable once ExplicitCallerInfoArgument
            set { SetProperty(value, onChanged: e => OnPropertyChanged(nameof(ShowCompactView))); }
        }

        public bool UseCompactView
        {
            get { return GetProperty(getDefault: () => SettingsHelper.Instance.Settings.UseCompactView); }
            set
            {
                SetProperty(value, onChanged: e =>
                {
                    SettingsHelper.Instance.Settings.UseCompactView = value;
                    // ReSharper disable once ExplicitCallerInfoArgument
                    OnPropertyChanged(nameof(ShowCompactView));
                });
            }
        }

        public bool ShowPersonalDps
        {
            get { return GetProperty(getDefault: () => SettingsHelper.Instance.Settings.ShowPersonalDps); }
            set { SetProperty(value, onChanged: e => SettingsHelper.Instance.Settings.ShowPersonalDps = value); }
        }

        public bool UseGlobalHotkeys
        {
            get { return GetProperty(getDefault: () => SettingsHelper.Instance.Settings.UseGlobalHotkeys); }
            set { SetProperty(value, onChanged: e => SettingsHelper.Instance.Settings.UseGlobalHotkeys = value); }
        }

        public bool UseRawSockets
        {
            get { return GetProperty(getDefault: () => SettingsHelper.Instance.Settings.UseRawSockets); }
            set
            {
                SetProperty(value, onChanged: e =>
                {
                    SettingsHelper.Instance.Settings.UseRawSockets = value;
                    Initialize();
                });
            }
        }

        #region Commands

        public RelayCommand ToggleIsPinnedCommand
        {
            get { return GetProperty(getDefault: () => new RelayCommand(ToggleIsPinned)); }
            set { SetProperty(value); }
        }

        public RelayCommand<DamageTracker> LoadEncounterCommand
        {
            get { return GetProperty(getDefault: () => new RelayCommand<DamageTracker>(LoadEncounter)); }
            set { SetProperty(value); }
        }

        public RelayCommand ClearEncountersCommand
        {
            get { return GetProperty(getDefault: () => new RelayCommand(ClearEncounters, () => ArchivedDamageTrackers.Count > 0)); }
            set { SetProperty(value); }
        }
        #endregion

        private object _snifferLock = new object();
        private bool _needInit;

        public void Initialize()
        {
            Task.Factory.StartNew(() =>
            {
                lock (_snifferLock)
                {
                    if (_teraSniffer != null)
                    {   //dereference the existing sniffer if it exists
                        var sniffer = _teraSniffer;
                        _teraSniffer = null;
                        sniffer.Enabled = false;
                        sniffer.MessageReceived -= HandleMessageReceived;
                        sniffer.NewConnection -= HandleNewConnection;
                        Logger.Info("Sniffer has been disabled.");
                    }

                    IpSniffer ipSniffer = null;
                    if (UseRawSockets)
                    {
                        ipSniffer = new IpSnifferRawSocketMultipleInterfaces();
                    }

                    _teraSniffer = new TeraSniffer(ipSniffer, BasicTeraData.Servers);
                    _teraSniffer.MessageReceived += HandleMessageReceived;
                    _teraSniffer.NewConnection += HandleNewConnection;
                    _teraSniffer.Enabled = true;

                    Logger.Info("Sniffer has been enabled.");
                }
            }, TaskCreationOptions.LongRunning);//provide hint to start on new thread
        }

        private void HandleNewConnection(Server server)
        {
            Server = server;
            _messageFactory = new MessageFactory();
            ResetDamageTracker();
            DamageTracker = DamageTracker ?? new DamageTracker
            {
                OnlyBosses = OnlyBosses,
                IgnoreOneshots = IgnoreOneshots
            };
            _needInit = true;
            Logger.Info($"Connected to server {server.Name}.");
        }

        private void ResetDamageTracker(ResetPlayerStatsMessage message = null)
        {
            if (Server == null) return;

            var saveEncounter = message != null && message.ShouldSaveCurrent;
            if (saveEncounter && !DamageTracker.IsArchived && DamageTracker.StatsByUser.Count > 0 && 
                DamageTracker.FirstAttack != null && DamageTracker.LastAttack != null)
            {
                DamageTracker.IsArchived = true;
                DamageTracker.Abnormals = _abnormalityStorage.Clone();
                ArchivedDamageTrackers.Add(DamageTracker);
                return;
            }
            if (message != null && !message.ShouldSaveCurrent && DamageTracker.IsArchived)
            {
                ArchivedDamageTrackers.Remove(DamageTracker);
            }

            _abnormalityStorage.ClearEnded();
            DamageTracker = new DamageTracker
            {
                OnlyBosses = OnlyBosses,
                IgnoreOneshots = IgnoreOneshots,
                Abnormals = _abnormalityStorage
            };
        }

        private void ExportStats(ExportStatsMessage message)
        {
            if (!DamageTracker.IsArchived)
                ResetDamageTracker(new ResetPlayerStatsMessage {ShouldSaveCurrent = true});
            DataExporter.ToTeraDpsApi(message.ExportType, DamageTracker, _teraData);
        }

        private void HandleMessageReceived(Message obj)
        {
            var message = _messageFactory.Create(obj);

            if (DamageTracker.IsArchived)
            { 
                var npcOccupier = message as SNpcOccupierInfo;
                if (npcOccupier != null)
                {
                    Entity ent = _entityTracker.GetOrPlaceholder(npcOccupier.NPC);
                    if (ent is NpcEntity)
                    {
                        var npce = ent as NpcEntity;
                        if (npce.Info.Boss && npcOccupier.Target != EntityId.Empty) 
                        {
                            CasualMessenger.Instance.ResetPlayerStats(true); //Stop viewing saved encounter on boss aggro
                        }
                    }
                    return;
                }
            }

            _entityTracker?.Update(message);
            var skillResultMessage = message as EachSkillResultServerMessage;
            if (skillResultMessage != null)
            {
                if (skillResultMessage.IsValid(DamageTracker))
                {
                    var skillResult = new SkillResult(skillResultMessage, _entityTracker, _playerTracker, _teraData.SkillDatabase, null, _abnormalityTracker);
                    CheckUpdate(skillResult);
                }
                return;
            }
            _playerTracker?.UpdateParty(message);
            _abnormalityTracker?.Update(message);
            var despawnNpc = message as SDespawnNpc;
            if (despawnNpc != null)
            {
                Entity ent = _entityTracker.GetOrPlaceholder(despawnNpc.Npc);
                if (ent is NpcEntity)
                {
                    var npce = ent as NpcEntity;
                    if (npce.Info.Boss && despawnNpc.Dead && !DamageTracker.IsArchived)
                    {   //no need to do something if we didn't count any skill against this boss
                        if (DamageTracker.StatsByUser.SelectMany(x => x.SkillLog).Any(x => x.Target == npce))
                        {
                            DamageTracker.PrimaryTarget = npce; //Name encounter with the last dead boss
                            DamageTracker.IsPrimaryTargetDead = despawnNpc.Dead;

                            //determine type
                            ExportType exportType = ExportType.None;
                            if (SettingsHelper.Instance.Settings.ExcelExport)
                                exportType = exportType | ExportType.Excel;
                            if (SettingsHelper.Instance.Settings.SiteExport)
                                exportType = exportType | ExportType.Upload;

                            if (exportType != ExportType.None)
                                DataExporter.ToTeraDpsApi(exportType, DamageTracker, _teraData);
                            if (AutosaveEncounters)
                                ResetDamageTracker(new ResetPlayerStatsMessage { ShouldSaveCurrent = true });
                        }
                    }
                }
                return;
            }
            var spawnNpc = message as SpawnNpcServerMessage;
            if (spawnNpc != null)
            {
                if (spawnNpc.NpcArea == 950 && spawnNpc.NpcId == 9501)
                {
                    var bosses = DamageTracker.StatsByUser.SelectMany(x => x.SkillLog).Select(x => x.Target).OfType<NpcEntity>().ToList(); 
                    var vergosPhase2Part1 = bosses.FirstOrDefault(x => x.Info.HuntingZoneId == 950 && x.Info.TemplateId == 1000);
                    var vergosPhase2Part2 = bosses.FirstOrDefault(x => x.Info.HuntingZoneId == 950 && x.Info.TemplateId == 2000);
                    //determine type
                    ExportType exportType = ExportType.None;
                    if (SettingsHelper.Instance.Settings.ExcelExport)
                        exportType = exportType | ExportType.Excel;
                    if (SettingsHelper.Instance.Settings.SiteExport)
                        exportType = exportType | ExportType.Upload;

                    if (exportType != ExportType.None)
                        DataExporter.ToTeraDpsApi(exportType, DamageTracker, _teraData, vergosPhase2Part1);
                        DataExporter.ToTeraDpsApi(exportType, DamageTracker, _teraData, vergosPhase2Part2);
                    if (AutosaveEncounters)
                        ResetDamageTracker(new ResetPlayerStatsMessage { ShouldSaveCurrent = true });
                }
                if (spawnNpc.NpcArea == 950 && spawnNpc.NpcId == 9502)
                {
                    var bosses = DamageTracker.StatsByUser.SelectMany(x => x.SkillLog).Select(x => x.Target).OfType<NpcEntity>().ToList();
                    var vergosPhase3 = bosses.FirstOrDefault(x => x.Info.HuntingZoneId == 950 && x.Info.TemplateId == 3000);
                    //determine type
                    ExportType exportType = ExportType.None;
                    if (SettingsHelper.Instance.Settings.ExcelExport)
                        exportType = exportType | ExportType.Excel;
                    if (SettingsHelper.Instance.Settings.SiteExport)
                        exportType = exportType | ExportType.Upload;

                    if (exportType != ExportType.None)
                        DataExporter.ToTeraDpsApi(exportType, DamageTracker, _teraData, vergosPhase3);
                    if (AutosaveEncounters)
                        ResetDamageTracker(new ResetPlayerStatsMessage { ShouldSaveCurrent = true });
                }
                return;
            }

            var sLogin = message as LoginServerMessage;
            if (sLogin != null)
            {
                if (_needInit)
                {
                    Server = BasicTeraData.Servers.GetServer(sLogin.ServerId, Server);
                    _messageFactory.Region = Server.Region;
                    Logger.Info($"Logged in to server {Server.Name}.");
                    _teraData = BasicTeraData.DataForRegion(Server.Region);
                    _entityTracker = new EntityTracker(_teraData.NpcDatabase, _userLogoTracker);
                    _playerTracker = new PlayerTracker(_entityTracker, BasicTeraData.Servers);
                    _abnormalityTracker = new AbnormalityTracker(_entityTracker, _playerTracker, _teraData.HotDotDatabase, _abnormalityStorage, CheckUpdate);
                    _entityTracker.Update(message);
                    _needInit = false;
                }
                _abnormalityStorage.EndAll(message.Time.Ticks);
                _abnormalityTracker = new AbnormalityTracker(_entityTracker, _playerTracker, _teraData.HotDotDatabase, _abnormalityStorage, CheckUpdate);
                return;
            }
            var cVersion = message as C_CHECK_VERSION;
            if (cVersion != null)
            {
                var opCodeNamer =
                    new OpCodeNamer(Path.Combine(BasicTeraData.ResourceDirectory,
                        $"opcodes/{cVersion.Versions[0]}.txt"));
                _messageFactory = new MessageFactory(opCodeNamer, Server.Region, cVersion.Versions[0]);
                return;
            }
            var sGetUserList = message as S_GET_USER_LIST;
            if (sGetUserList != null)
            {
                _userLogoTracker.SetUserList(sGetUserList);
                return;
            }
        }

        private bool IsInactiveTimerReached()
        {
            return SettingsHelper.Instance.Settings.InactivityResetDuration > 0 
                && _inactivityTimer.Elapsed >
                   TimeSpan.FromSeconds(SettingsHelper.Instance.Settings.InactivityResetDuration);
        }

        private void CheckUpdate(SkillResult skillResult)
        {
            if (PartyOnly &&//check if party only
                !(_playerTracker.MyParty(skillResult.SourcePlayer) || _playerTracker.MyParty(skillResult.TargetPlayer)))
                return;
            if (IsInactiveTimerReached() && skillResult.IsValid())
            {
                CasualMessenger.Instance.ResetPlayerStats(AutosaveEncounters || DamageTracker.IsArchived);
            }
            if (!DamageTracker.IsArchived && skillResult.IsValid(DamageTracker?.FirstAttack)) //don't process while viewing a past encounter
            {
                DamageTracker.Update(skillResult);
                if (!skillResult.IsHeal && skillResult.Amount > 0)
                    _inactivityTimer.Restart();
                PlayerCount = DamageTracker.StatsByUser.Count;
            }
        }

        private void PasteStats(PastePlayerStatsMessage obj)
        {
            if (DamageTracker == null) return;

            var playerStatsSequence = DamageTracker.StatsByUser.OrderByDescending(playerStats => playerStats.Dealt.Damage).TakeWhile(x => x.Dealt.Damage > 0);
            const int maxLength = 300;

            var sb = new StringBuilder();
            bool first = true;

            string body = SettingsHelper.Instance.Settings.DpsPasteFormat;
            if (body.Contains('@'))
            {
                var splitter = body.Split(new[] { '@' }, 2);                
                var placeHolder = new DamageTrackerFormatter(DamageTracker, FormatHelpers.Invariant);
                sb.Append(placeHolder.Replace(splitter[0]));
                body = splitter[1];
            }
            foreach (var playerInfo in playerStatsSequence)
            {
                var placeHolder = new PlayerStatsFormatter(playerInfo, _teraData, FormatHelpers.Invariant);
                var playerText = first ? "" : " | ";

                playerText += placeHolder.Replace(body);

                if (sb.Length + playerText.Length > maxLength)
                    break;

                sb.Append(playerText);
                first = false;
            }
            
            if (sb.Length > 0)
            {
                var text = sb.ToString();
                var isActive = ProcessHelper.Instance.IsTeraActive;
                if (isActive.HasValue && isActive.Value)
                {
                    //send text input to Tera
                    ProcessHelper.Instance.SendString(text);
                }
                //copy to clipboard in case user wants to paste outside of Tera
                Application.Current.Dispatcher.Invoke(() => Clipboard.SetDataObject(text));
            }
        }

        private void LoadEncounter(DamageTracker obj)
        {
            DamageTracker = obj;
        }

        private void ClearEncounters()
        {
            ArchivedDamageTrackers.Clear();
        }

        private void ToggleIsPinned()
        {
            IsPinned = !IsPinned;
        }
    }
}
