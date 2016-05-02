using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using CasualMeter.Common.Conductors;
using CasualMeter.Common.Conductors.Messages;
using CasualMeter.Common.Formatters;
using CasualMeter.Common.Helpers;
using CasualMeter.Common.UI.ViewModels;
using CasualMeter.Common.TeraDpsApi;
using GalaSoft.MvvmLight.CommandWpf;
using log4net;
using Lunyx.Common.UI.Wpf;
using NetworkSniffer;
using Tera;
using Tera.DamageMeter;
using Tera.Data;
using Tera.Game;
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
        private AbnormalityTracker _abnormalityTracker;
        private CharmTracker _charmTracker;
        private readonly AbnormalityStorage _abnormalityStorage = new AbnormalityStorage();

        private Stopwatch _inactivityTimer = new Stopwatch();

        public ShellViewModel()
        {
            CasualMessenger.Instance.Messenger.Register<PastePlayerStatsMessage>(this, PasteStats);
            CasualMessenger.Instance.Messenger.Register<ResetPlayerStatsMessage>(this, ResetDamageTracker);
        }

        #region Properties

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

        public ThreadSafeObservableCollection<DamageTracker> ArchivedDamageTrackers
        {
            get { return GetProperty(getDefault: () => new ThreadSafeObservableCollection<DamageTracker>()); }
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

        public bool Excel
        {
            get { return GetProperty(getDefault: () => SettingsHelper.Instance.Settings.Excel); }
            set { SetProperty(value, onChanged: e => SettingsHelper.Instance.Settings.Excel = value); }
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
            _teraData = BasicTeraData.DataForRegion(server.Region);
            _entityTracker = new EntityTracker(_teraData.NpcDatabase);
            _playerTracker = new PlayerTracker(_entityTracker,BasicTeraData.Servers);
            _messageFactory = new MessageFactory(_teraData.OpCodeNamer);

            ResetDamageTracker();
            DamageTracker = DamageTracker ?? new DamageTracker
            {
                OnlyBosses = OnlyBosses,
                IgnoreOneshots = IgnoreOneshots
            };
            _abnormalityTracker = new AbnormalityTracker(_entityTracker, _playerTracker, _teraData.HotDotDatabase, _abnormalityStorage, CheckUpdate);
            _charmTracker = new CharmTracker(_abnormalityTracker);
            Logger.Info($"Connected to server {server.Name}.");
        }

        private void ResetDamageTracker(ResetPlayerStatsMessage message = null)
        {
            if (Server == null) return;

            bool saveEncounter = message != null && message.ShouldSaveCurrent;
            if (saveEncounter && !DamageTracker.IsArchived && DamageTracker.StatsByUser.Count > 0 && 
                DamageTracker.FirstAttack != null && DamageTracker.LastAttack != null)
            {
                DamageTracker.IsArchived = true;
                DamageTracker.abnormals = _abnormalityStorage.Clone();
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
                abnormals = _abnormalityStorage
            };
        }

        private void HandleMessageReceived(Message obj)
        {
            var message = _messageFactory.Create(obj);

            var despawnNpc = message as SDespawnNpc;
            if (despawnNpc != null)
            {
                Entity ent = _entityTracker.GetOrPlaceholder(despawnNpc.NPC);
                if (ent is NpcEntity)
                {
                    _abnormalityTracker.StopAggro(despawnNpc);
                    _abnormalityTracker.DeleteAbnormality(despawnNpc);
                    var npce = ent as NpcEntity;
                    if (npce.Info.Boss && despawnNpc.Dead && !DamageTracker.IsArchived)
                    {
                        if (DamageTracker.StatsByUser.SelectMany(x => x.SkillLog).Any(x => x.Target == npce)) //no need to do something if we didn't count any skill against this boss
                        {
                            DataExporter.ToTeraDpsApi(despawnNpc, DamageTracker, _entityTracker, _teraData);
                            DamageTracker.Name = npce.Info.Name; //Name encounter with the last dead boss
                            if (AutosaveEncounters) CasualMessenger.Instance.ResetPlayerStats(true);
                        }
                    }
                }
                return;
            }
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

            var skillResultMessage = message as EachSkillResultServerMessage;
            if (skillResultMessage != null)
            {
                if (skillResultMessage.IsValid(DamageTracker))
                {
                    var skillResult = new SkillResult(skillResultMessage, _entityTracker, _playerTracker, _teraData.SkillDatabase);
                    CheckUpdate(skillResult);
                }
                return;
            }

            var changeHp = message as SCreatureChangeHp;
            if (changeHp != null)
            {
                _abnormalityTracker.Update(changeHp);
                return;
            }

            var pchangeHp = message as SPartyMemberChangeHp;
            if (pchangeHp != null)
            {
                var user = _playerTracker.GetOrNull(pchangeHp.ServerId, pchangeHp.PlayerId);
                if(user==null) return;
                _abnormalityTracker.RegisterSlaying(user.User, pchangeHp.Slaying, pchangeHp.Time.Ticks);
                return;
            }

            var pmstatupd = message as S_PARTY_MEMBER_STAT_UPDATE;
            if (pmstatupd != null)
            {
                var user = _playerTracker.GetOrNull(pmstatupd.ServerId, pmstatupd.PlayerId);
                if (user == null) return;
                _abnormalityTracker.RegisterSlaying(user.User, pmstatupd.Slaying, pmstatupd.Time.Ticks);
                return;
            }

            var pstatupd = message as S_PLAYER_STAT_UPDATE;
            if (pstatupd != null)
            {
                _abnormalityTracker.RegisterSlaying(_entityTracker.MeterUser, pstatupd.Slaying, pstatupd.Time.Ticks);
                return;
            }

            var changeMp = message as SPlayerChangeMp;
            if (changeMp != null)
            {
                _abnormalityTracker.Update(changeMp);
                return;
            }

            var npcStatus = message as SNpcStatus;
            if (npcStatus != null)
            {
                _abnormalityTracker.RegisterNpcStatus(npcStatus);
                return;
            }

            var dead = message as SCreatureLife;
            if (dead != null)
            {
                _abnormalityTracker.RegisterDead(dead);
                return;
            }

            var abnormalityBegin = message as SAbnormalityBegin;
            if (abnormalityBegin != null)
            {
                _abnormalityTracker.AddAbnormality(abnormalityBegin);
                return;
            }

            var abnormalityEnd = message as SAbnormalityEnd;
            if (abnormalityEnd != null)
            {
                _abnormalityTracker.DeleteAbnormality(abnormalityEnd);
                return;
            }

            var abnormalityRefresh = message as SAbnormalityRefresh;
            if (abnormalityRefresh != null)
            {
                _abnormalityTracker.RefreshAbnormality(abnormalityRefresh);
                return;
            }

            var despawnUser = message as SDespawnUser;
            if (despawnUser != null)
            {
                _charmTracker.CharmReset(despawnUser.User, new List<CharmStatus>(), despawnUser.Time.Ticks);
                _abnormalityTracker.DeleteAbnormality(despawnUser);
                return;
            }

            var charmEnable = message as SEnableCharmStatus;
            if (charmEnable != null)
            {
                _charmTracker.CharmEnable(_entityTracker.MeterUser.Id, charmEnable.CharmId, charmEnable.Time.Ticks);
                return;
            }
            var pcharmEnable = message as SPartyMemberCharmEnable;
            if (pcharmEnable != null)
            {
                var player = _playerTracker.GetOrNull(pcharmEnable.ServerId, pcharmEnable.PlayerId);
                if (player == null) return;
                _charmTracker.CharmEnable(player.User.Id, pcharmEnable.CharmId, pcharmEnable.Time.Ticks);
                return;
            }
            var charmReset = message as SResetCharmStatus;
            if (charmReset != null)
            {
                _charmTracker.CharmReset(charmReset.TargetId, charmReset.Charms, charmReset.Time.Ticks);
                return;
            }
            var pcharmReset = message as SPartyMemberCharmReset;
            if (pcharmReset != null)
            {
                var player = _playerTracker.GetOrNull(pcharmReset.ServerId, pcharmReset.PlayerId);
                if (player == null) return;
                _charmTracker.CharmReset(player.User.Id, pcharmReset.Charms, pcharmReset.Time.Ticks);
                return;
            }
            var charmDel = message as SRemoveCharmStatus;
            if (charmDel != null)
            {
                _charmTracker.CharmDel(_entityTracker.MeterUser.Id, charmDel.CharmId, charmDel.Time.Ticks);
                return;
            }
            var pcharmDel = message as SPartyMemberCharmDel;
            if (pcharmDel != null)
            {
                var player = _playerTracker.GetOrNull(pcharmDel.ServerId, pcharmDel.PlayerId);
                if (player == null) return;
                _charmTracker.CharmDel(player.User.Id, pcharmDel.CharmId, pcharmDel.Time.Ticks);
                return;
            }
            var charmAdd = message as SAddCharmStatus;
            if (charmAdd != null)
            {
                _charmTracker.CharmAdd(charmAdd.TargetId, charmAdd.CharmId, charmAdd.Status, charmAdd.Time.Ticks);
                return;
            }
            var pcharmAdd = message as SPartyMemberCharmAdd;
            if (pcharmAdd != null)
            {
                var player = _playerTracker.GetOrNull(pcharmAdd.ServerId, pcharmAdd.PlayerId);
                if (player == null) return;
                _charmTracker.CharmAdd(player.User.Id, pcharmAdd.CharmId, pcharmAdd.Status, pcharmAdd.Time.Ticks);
                return;
            }

            _entityTracker.Update(message);
            _playerTracker.UpdateParty(message);

            var spawnMe = message as SpawnMeServerMessage;
            if (spawnMe != null)
            {
                _abnormalityStorage.EndAll(message.Time.Ticks);
                _abnormalityTracker = new AbnormalityTracker(_entityTracker, _playerTracker, _teraData.HotDotDatabase, _abnormalityStorage, CheckUpdate);
                _charmTracker = new CharmTracker(_abnormalityTracker);
                return;
            }
            var sLogin = message as LoginServerMessage;
            if (sLogin != null)
            {
                _abnormalityStorage.EndAll(message.Time.Ticks);
                _abnormalityTracker = new AbnormalityTracker(_entityTracker, _playerTracker, _teraData.HotDotDatabase, _abnormalityStorage, CheckUpdate);
                _charmTracker = new CharmTracker(_abnormalityTracker);
                Server = BasicTeraData.Servers.GetServer(sLogin.ServerId,Server);
                return;
            }
        }

        public void CheckUpdate(SkillResult skillResult)
        {
            if (PartyOnly && !(_playerTracker.MyParty(skillResult.SourcePlayer) || _playerTracker.MyParty(skillResult.TargetPlayer))) return;
            if (SettingsHelper.Instance.Settings.InactivityResetDuration > 0
            && _inactivityTimer.Elapsed > TimeSpan.FromSeconds(SettingsHelper.Instance.Settings.InactivityResetDuration)
            && skillResult.IsValid())
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
