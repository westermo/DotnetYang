using Ietf.Alarms;

namespace YangSource;

public class ExampleYangServer : IYangServer
{
    YangNode.AlarmsContainer.AlarmListContainer.AlarmEntry.SetOperatorStateInput? m_operatorState;
    public Task OnSetOperatorState(YangNode.AlarmsContainer.AlarmListContainer.AlarmEntry.SetOperatorStateInput input)
    {
        m_operatorState = input;
        return Task.CompletedTask;
    }

    public Task<YangNode.AlarmsContainer.AlarmListContainer.PurgeAlarmsOutput> OnPurgeAlarms(
        YangNode.AlarmsContainer.AlarmListContainer.PurgeAlarmsInput input)
    {
        throw new NotImplementedException();
    }

    public Task<YangNode.AlarmsContainer.AlarmListContainer.CompressAlarmsOutput> OnCompressAlarms(
        YangNode.AlarmsContainer.AlarmListContainer.CompressAlarmsInput input)
    {
        throw new NotImplementedException();
    }

    public Task<YangNode.AlarmsContainer.ShelvedAlarmsContainer.PurgeShelvedAlarmsOutput> OnPurgeShelvedAlarms(
        YangNode.AlarmsContainer.ShelvedAlarmsContainer.PurgeShelvedAlarmsInput input)
    {
        throw new NotImplementedException();
    }

    public Task<YangNode.AlarmsContainer.ShelvedAlarmsContainer.CompressShelvedAlarmsOutput> OnCompressShelvedAlarms(
        YangNode.AlarmsContainer.ShelvedAlarmsContainer.CompressShelvedAlarmsInput input)
    {
        throw new NotImplementedException();
    }

    public Task OnOperatorAction(YangNode.AlarmsContainer.AlarmListContainer.AlarmEntry.OperatorAction notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnAlarmNotification(YangNode.AlarmNotification notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnAlarmInventoryChanged(YangNode.AlarmInventoryChanged notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnL2TerminationPointEvent(Ietf.L2.Topology.State.YangNode.L2TerminationPointEvent notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task<Ietf.Interfaces.YangNode.InterfacesContainer.InterfaceEntry.LinkOamContainer.RemoteLoopbackOutput>
        OnRemoteLoopback(
            Ietf.Interfaces.YangNode.InterfacesContainer.InterfaceEntry.LinkOamContainer.RemoteLoopbackInput input)
    {
        throw new NotImplementedException();
    }

    public Task<Ietf.Interfaces.YangNode.InterfacesContainer.InterfaceEntry.LinkOamContainer.ResetStatsOutput>
        OnResetStats(Ietf.Interfaces.YangNode.InterfacesContainer.InterfaceEntry.LinkOamContainer target)
    {
        throw new NotImplementedException();
    }

    public Task OnNonThresholdEvent(Ietf.Interfaces.YangNode.InterfacesContainer.InterfaceEntry.LinkOamContainer.NonThresholdEvent notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnThresholdEvent(Ietf.Interfaces.YangNode.InterfacesContainer.InterfaceEntry.LinkOamContainer.ThresholdEvent notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnNonThresholdEvent(
        Ietf.Interfaces.YangNode.InterfacesContainer.InterfaceEntry.LinkOamContainer.NonThresholdEvent notification)
    {
        throw new NotImplementedException();
    }

    public Task OnThresholdEvent(
        Ietf.Interfaces.YangNode.InterfacesContainer.InterfaceEntry.LinkOamContainer.ThresholdEvent notification)
    {
        throw new NotImplementedException();
    }

    public Task OnMepFaultAlarm(
        Ieee802.Dot1q.Cfm.YangNode.CfmContainer.MaintenanceGroupEntry.MepEntry.MepFaultAlarm notification)
    {
        throw new NotImplementedException();
    }

    public Task OnNonSuccessCodeSent(Ietf.Dhcpv6.Server.YangNode.NonSuccessCodeSent notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnManualSwitchWorking(
        Ietf.Microwave.Radio.Link.YangNode.RadioLinkProtectionGroupsContainer.ProtectionGroupEntry target)
    {
        throw new NotImplementedException();
    }

    public Task OnManualSwitchProtection(
        Ietf.Microwave.Radio.Link.YangNode.RadioLinkProtectionGroupsContainer.ProtectionGroupEntry target)
    {
        throw new NotImplementedException();
    }

    public Task OnForcedSwitch(
        Ietf.Microwave.Radio.Link.YangNode.RadioLinkProtectionGroupsContainer.ProtectionGroupEntry target)
    {
        throw new NotImplementedException();
    }

    public Task OnLockoutOfProtection(
        Ietf.Microwave.Radio.Link.YangNode.RadioLinkProtectionGroupsContainer.ProtectionGroupEntry target)
    {
        throw new NotImplementedException();
    }

    public Task OnFreeze(
        Ietf.Microwave.Radio.Link.YangNode.RadioLinkProtectionGroupsContainer.ProtectionGroupEntry target)
    {
        throw new NotImplementedException();
    }

    public Task OnExercise(
        Ietf.Microwave.Radio.Link.YangNode.RadioLinkProtectionGroupsContainer.ProtectionGroupEntry target)
    {
        throw new NotImplementedException();
    }

    public Task OnClear(
        Ietf.Microwave.Radio.Link.YangNode.RadioLinkProtectionGroupsContainer.ProtectionGroupEntry target)
    {
        throw new NotImplementedException();
    }

    public Task OnNatPoolEvent(Ietf.Nat.YangNode.NatPoolEvent notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnNatInstanceEvent(Ietf.Nat.YangNode.NatInstanceEvent notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnMepFaultAlarm(Ieee802.Dot1q.Cfm.YangNode.CfmContainer.MaintenanceGroupEntry.MepEntry.MepFaultAlarm notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnRemoteTableChange(Ieee802.Dot1ab.Lldp.YangNode.RemoteTableChange notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task<Ietf.Connection.Oriented.Oam.YangNode.ContinuityCheckOutput> OnContinuityCheck(
        Ietf.Connection.Oriented.Oam.YangNode.ContinuityCheckInput input)
    {
        throw new NotImplementedException();
    }

    public Task<Ietf.Connection.Oriented.Oam.YangNode.ContinuityVerificationOutput> OnContinuityVerification(
        Ietf.Connection.Oriented.Oam.YangNode.ContinuityVerificationInput input)
    {
        throw new NotImplementedException();
    }

    public Task<Ietf.Connection.Oriented.Oam.YangNode.TracerouteOutput> OnTraceroute(
        Ietf.Connection.Oriented.Oam.YangNode.TracerouteInput input)
    {
        return Task.FromResult(new Ietf.Connection.Oriented.Oam.YangNode.TracerouteOutput
        {
            Response = new List<Ietf.Connection.Oriented.Oam.YangNode.TracerouteOutput.ResponseEntry>
            {
                new()
                {
                    Mip = new Ietf.Connection.Oriented.Oam.YangNode.TracerouteOutput.ResponseEntry.MipContainer
                    {
                        MipAddress =
                            new Ietf.Connection.Oriented.Oam.YangNode.TracerouteOutput.ResponseEntry.MipContainer.
                                MipAddressChoice
                                {
                                    IpAddressCaseValue =
                                        new Ietf.Connection.Oriented.Oam.YangNode.TracerouteOutput.ResponseEntry.
                                            MipContainer.MipAddressChoice.IpAddressCaseValueCase
                                            {
                                                IpAddress = new Ietf.Inet.Types.YangNode.IpAddress(
                                                    new Ietf.Inet.Types.YangNode.Ipv4Address("12.23.34.45"))
                                            }
                                }
                    }
                },
                new()
                {
                    Ttl = 1,
                    MonitorStats =
                        new Ietf.Connection.Oriented.Oam.YangNode.TracerouteOutput.ResponseEntry.MonitorStatsChoice
                        {
                            MonitorNullCaseValue =
                                new Ietf.Connection.Oriented.Oam.YangNode.TracerouteOutput.ResponseEntry.
                                    MonitorStatsChoice.
                                    MonitorNullCaseValueCase
                                    {
                                        MonitorNull = new object()
                                    }
                        }
                }
            }
        });
    }

    public Task OnDefectConditionNotification(Ietf.Connection.Oriented.Oam.YangNode.DefectConditionNotification notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnDefectClearedNotification(Ietf.Connection.Oriented.Oam.YangNode.DefectClearedNotification notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnSoftwireBindingInstanceEvent(Ietf.Softwire.Br.YangNode.SoftwireBindingInstanceEvent notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnSoftwireAlgorithmInstanceEvent(Ietf.Softwire.Br.YangNode.SoftwireAlgorithmInstanceEvent notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task<Ietf.Routing.YangNode.RoutingContainer.RibsContainer.RibEntry.ActiveRouteOutput> OnActiveRoute(
        Ietf.Routing.YangNode.RoutingContainer.RibsContainer.RibEntry target)
    {
        throw new NotImplementedException();
    }

    public Task<Ietf.Routing.YangNode.RoutingStateContainer.RibsContainer.RibEntry.ActiveRouteOutput> OnActiveRoute(
        Ietf.Routing.YangNode.RoutingStateContainer.RibsContainer.RibEntry target)
    {
        throw new NotImplementedException();
    }

    public Task<Ietf.I2rs.Rib.YangNode.RibAddOutput> OnRibAdd(Ietf.I2rs.Rib.YangNode.RibAddInput input)
    {
        throw new NotImplementedException();
    }

    public Task<Ietf.I2rs.Rib.YangNode.RibDeleteOutput> OnRibDelete(Ietf.I2rs.Rib.YangNode.RibDeleteInput input)
    {
        throw new NotImplementedException();
    }

    public Task<Ietf.I2rs.Rib.YangNode.RouteAddOutput> OnRouteAdd(Ietf.I2rs.Rib.YangNode.RouteAddInput input)
    {
        throw new NotImplementedException();
    }

    public Task<Ietf.I2rs.Rib.YangNode.RouteDeleteOutput> OnRouteDelete(Ietf.I2rs.Rib.YangNode.RouteDeleteInput input)
    {
        throw new NotImplementedException();
    }

    public Task<Ietf.I2rs.Rib.YangNode.RouteUpdateOutput> OnRouteUpdate(Ietf.I2rs.Rib.YangNode.RouteUpdateInput input)
    {
        throw new NotImplementedException();
    }

    public Task<Ietf.I2rs.Rib.YangNode.NhAddOutput> OnNhAdd(Ietf.I2rs.Rib.YangNode.NhAddInput input)
    {
        throw new NotImplementedException();
    }

    public Task<Ietf.I2rs.Rib.YangNode.NhDeleteOutput> OnNhDelete(Ietf.I2rs.Rib.YangNode.NhDeleteInput input)
    {
        throw new NotImplementedException();
    }

    public Task OnNexthopResolutionStatusChange(Ietf.I2rs.Rib.YangNode.NexthopResolutionStatusChange notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnRouteChange(Ietf.I2rs.Rib.YangNode.RouteChange notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnNexthopResolutionStatusChange(Ietf.I2rs.Rib.YangNode.NexthopResolutionStatusChange notification)
    {
        throw new NotImplementedException();
    }

    public Task OnRouteChange(Ietf.I2rs.Rib.YangNode.RouteChange notification)
    {
        throw new NotImplementedException();
    }

    public Task OnRemoteTableChange(Ieee802.Dot1ab.Lldp.YangNode.RemoteTableChange notification)
    {
        throw new NotImplementedException();
    }

    public Task OnClearNeighbor(Ietf.Ospf.YangNode.ClearNeighborInput input)
    {
        throw new NotImplementedException();
    }

    public Task OnClearDatabase(Ietf.Ospf.YangNode.ClearDatabaseInput input)
    {
        throw new NotImplementedException();
    }

    public Task OnIfStateChange(Ietf.Ospf.YangNode.IfStateChange notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnIfConfigError(Ietf.Ospf.YangNode.IfConfigError notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnNbrStateChange(Ietf.Ospf.YangNode.NbrStateChange notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnNbrRestartHelperStatusChange(Ietf.Ospf.YangNode.NbrRestartHelperStatusChange notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnIfRxBadPacket(Ietf.Ospf.YangNode.IfRxBadPacket notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnLsdbApproachingOverflow(Ietf.Ospf.YangNode.LsdbApproachingOverflow notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnLsdbOverflow(Ietf.Ospf.YangNode.LsdbOverflow notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnNssaTranslatorStatusChange(Ietf.Ospf.YangNode.NssaTranslatorStatusChange notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnRestartStatusChange(Ietf.Ospf.YangNode.RestartStatusChange notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnIfStateChange(Ietf.Ospf.YangNode.IfStateChange notification)
    {
        throw new NotImplementedException();
    }

    public Task OnIfConfigError(Ietf.Ospf.YangNode.IfConfigError notification)
    {
        throw new NotImplementedException();
    }

    public Task OnNbrStateChange(Ietf.Ospf.YangNode.NbrStateChange notification)
    {
        throw new NotImplementedException();
    }

    public Task OnNbrRestartHelperStatusChange(Ietf.Ospf.YangNode.NbrRestartHelperStatusChange notification)
    {
        throw new NotImplementedException();
    }

    public Task OnIfRxBadPacket(Ietf.Ospf.YangNode.IfRxBadPacket notification)
    {
        throw new NotImplementedException();
    }

    public Task OnLsdbApproachingOverflow(Ietf.Ospf.YangNode.LsdbApproachingOverflow notification)
    {
        throw new NotImplementedException();
    }

    public Task OnLsdbOverflow(Ietf.Ospf.YangNode.LsdbOverflow notification)
    {
        throw new NotImplementedException();
    }

    public Task OnNssaTranslatorStatusChange(Ietf.Ospf.YangNode.NssaTranslatorStatusChange notification)
    {
        throw new NotImplementedException();
    }

    public Task OnRestartStatusChange(Ietf.Ospf.YangNode.RestartStatusChange notification)
    {
        throw new NotImplementedException();
    }

    public Task OnTerminationPointEvent(Ietf.L3.Unicast.Topology.State.YangNode.TerminationPointEvent notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task<Ieee802.Dot1q.Cfm.YangNode.CfmContainer.MaintenanceGroupEntry.MepEntry.TransmitLoopbackOutput>
        OnTransmitLoopback(
            Ieee802.Dot1q.Cfm.YangNode.CfmContainer.MaintenanceGroupEntry.MepEntry.TransmitLoopbackInput input)
    {
        throw new NotImplementedException();
    }

    public Task<Ieee802.Dot1q.Cfm.YangNode.CfmContainer.MaintenanceGroupEntry.MepEntry.TransmitLinktraceOutput>
        OnTransmitLinktrace(
            Ieee802.Dot1q.Cfm.YangNode.CfmContainer.MaintenanceGroupEntry.MepEntry.TransmitLinktraceInput input)
    {
        throw new NotImplementedException();
    }

    public Task OnMplsNotification(Ietf.Bfd.Mpls.YangNode.MplsNotification notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnBindLneNameFailed(Ietf.Logical.Network.Element.YangNode.BindLneNameFailed notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnSoftwireBindingInstanceEvent(Ietf.Softwire.Br.YangNode.SoftwireBindingInstanceEvent notification)
    {
        throw new NotImplementedException();
    }

    public Task OnSoftwireAlgorithmInstanceEvent(Ietf.Softwire.Br.YangNode.SoftwireAlgorithmInstanceEvent notification)
    {
        throw new NotImplementedException();
    }

    public Task OnStatisticsReset(Ietf.Ntp.YangNode.StatisticsResetInput input)
    {
        throw new NotImplementedException();
    }

    public Task<Ietf.Nmda.Compare.YangNode.CompareOutput> OnCompare(Ietf.Nmda.Compare.YangNode.CompareInput input)
    {
        throw new NotImplementedException();
    }

    public Task OnL3NodeEvent(Ietf.L3.Unicast.Topology.State.YangNode.L3NodeEvent notification)
    {
        throw new NotImplementedException();
    }

    public Task OnL3LinkEvent(Ietf.L3.Unicast.Topology.State.YangNode.L3LinkEvent notification)
    {
        throw new NotImplementedException();
    }

    public Task OnL3PrefixEvent(Ietf.L3.Unicast.Topology.State.YangNode.L3PrefixEvent notification)
    {
        throw new NotImplementedException();
    }

    public Task OnTerminationPointEvent(Ietf.L3.Unicast.Topology.State.YangNode.TerminationPointEvent notification)
    {
        throw new NotImplementedException();
    }

    public Task<Ietf.Sztp.Bootstrap.Server.YangNode.GetBootstrappingDataOutput> OnGetBootstrappingData(
        Ietf.Sztp.Bootstrap.Server.YangNode.GetBootstrappingDataInput input)
    {
        throw new NotImplementedException();
    }

    public Task OnReportProgress(Ietf.Sztp.Bootstrap.Server.YangNode.ReportProgressInput input)
    {
        throw new NotImplementedException();
    }

    public Task OnL3NodeEvent(Ietf.L3.Unicast.Topology.State.YangNode.L3NodeEvent notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnL3LinkEvent(Ietf.L3.Unicast.Topology.State.YangNode.L3LinkEvent notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnL3PrefixEvent(Ietf.L3.Unicast.Topology.State.YangNode.L3PrefixEvent notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnSetCurrentDatetime(Ietf.System.YangNode.SetCurrentDatetimeInput input)
    {
        throw new NotImplementedException();
    }

    public Task OnSystemRestart()
    {
        throw new NotImplementedException();
    }

    public Task OnSystemShutdown()
    {
        throw new NotImplementedException();
    }

    public Task OnMplsNotification(Ietf.Bfd.Mpls.YangNode.MplsNotification notification)
    {
        throw new NotImplementedException();
    }

    public Task<Ieee1906.Dot1.Function.YangNode.CallOutput> OnCall(Ieee1906.Dot1.Function.YangNode.CallInput input)
    {
        throw new NotImplementedException();
    }

    public Task OnB4AddressChangeLimitPolicyViolation(Ietf.Dslite.YangNode.B4AddressChangeLimitPolicyViolation notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnL2NodeEvent(Ietf.L2.Topology.State.YangNode.L2NodeEvent notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnL2LinkEvent(Ietf.L2.Topology.State.YangNode.L2LinkEvent notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnInvalidIaAddressDetected(Ietf.Dhcpv6.Client.YangNode.InvalidIaAddressDetected notification)
    {
        throw new NotImplementedException();
    }

    public Task OnTransmissionFailed(Ietf.Dhcpv6.Client.YangNode.TransmissionFailed notification)
    {
        throw new NotImplementedException();
    }

    public Task OnUnsuccessfulStatusCode(Ietf.Dhcpv6.Client.YangNode.UnsuccessfulStatusCode notification)
    {
        throw new NotImplementedException();
    }

    public Task OnServerDuidChanged(Ietf.Dhcpv6.Client.YangNode.ServerDuidChanged notification)
    {
        throw new NotImplementedException();
    }

    public Task OnBindLneNameFailed(Ietf.Logical.Network.Element.YangNode.BindLneNameFailed notification)
    {
        throw new NotImplementedException();
    }

    public Task OnB4AddressChangeLimitPolicyViolation(
        Ietf.Dslite.YangNode.B4AddressChangeLimitPolicyViolation notification)
    {
        throw new NotImplementedException();
    }

    public Task OnL2NodeEvent(Ietf.L2.Topology.State.YangNode.L2NodeEvent notification)
    {
        throw new NotImplementedException();
    }

    public Task OnL2LinkEvent(Ietf.L2.Topology.State.YangNode.L2LinkEvent notification)
    {
        throw new NotImplementedException();
    }

    public Task OnL2TerminationPointEvent(Ietf.L2.Topology.State.YangNode.L2TerminationPointEvent notification)
    {
        throw new NotImplementedException();
    }

    public Task<Ietf.Subscribed.Notifications.YangNode.EstablishSubscriptionOutput> OnEstablishSubscription(
        Ietf.Subscribed.Notifications.YangNode.EstablishSubscriptionInput input)
    {
        throw new NotImplementedException();
    }

    public Task OnModifySubscription(Ietf.Subscribed.Notifications.YangNode.ModifySubscriptionInput input)
    {
        throw new NotImplementedException();
    }

    public Task OnDeleteSubscription(Ietf.Subscribed.Notifications.YangNode.DeleteSubscriptionInput input)
    {
        throw new NotImplementedException();
    }

    public Task OnKillSubscription(Ietf.Subscribed.Notifications.YangNode.KillSubscriptionInput input)
    {
        throw new NotImplementedException();
    }

    public Task<Ietf.Subscribed.Notifications.YangNode.SubscriptionsContainer.SubscriptionEntry.ReceiversContainer.
        ReceiverEntry.ResetOutput> OnReset(
        Ietf.Subscribed.Notifications.YangNode.SubscriptionsContainer.SubscriptionEntry.ReceiversContainer.ReceiverEntry
            target)
    {
        throw new NotImplementedException();
    }

    public Task OnReplayCompleted(Ietf.Subscribed.Notifications.YangNode.ReplayCompleted notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnSubscriptionCompleted(Ietf.Subscribed.Notifications.YangNode.SubscriptionCompleted notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnSubscriptionModified(Ietf.Subscribed.Notifications.YangNode.SubscriptionModified notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnSubscriptionResumed(Ietf.Subscribed.Notifications.YangNode.SubscriptionResumed notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnSubscriptionStarted(Ietf.Subscribed.Notifications.YangNode.SubscriptionStarted notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnSubscriptionSuspended(Ietf.Subscribed.Notifications.YangNode.SubscriptionSuspended notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnSubscriptionTerminated(Ietf.Subscribed.Notifications.YangNode.SubscriptionTerminated notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnInvalidIaAddressDetected(Ietf.Dhcpv6.Client.YangNode.InvalidIaAddressDetected notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnTransmissionFailed(Ietf.Dhcpv6.Client.YangNode.TransmissionFailed notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnUnsuccessfulStatusCode(Ietf.Dhcpv6.Client.YangNode.UnsuccessfulStatusCode notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnServerDuidChanged(Ietf.Dhcpv6.Client.YangNode.ServerDuidChanged notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnSegmentRoutingSrgbCollision(Ietf.Segment.Routing.Mpls.YangNode.SegmentRoutingSrgbCollision notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnSegmentRoutingGlobalSidCollision(Ietf.Segment.Routing.Mpls.YangNode.SegmentRoutingGlobalSidCollision notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnSegmentRoutingIndexOutOfRange(Ietf.Segment.Routing.Mpls.YangNode.SegmentRoutingIndexOutOfRange notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnHardwareStateChange(Ietf.Hardware.YangNode.HardwareStateChange notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnHardwareStateOperEnabled(Ietf.Hardware.YangNode.HardwareStateOperEnabled notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnHardwareStateOperDisabled(Ietf.Hardware.YangNode.HardwareStateOperDisabled notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnReplayCompleted(Ietf.Subscribed.Notifications.YangNode.ReplayCompleted notification)
    {
        throw new NotImplementedException();
    }

    public Task OnSubscriptionCompleted(Ietf.Subscribed.Notifications.YangNode.SubscriptionCompleted notification)
    {
        throw new NotImplementedException();
    }

    public Task OnSubscriptionModified(Ietf.Subscribed.Notifications.YangNode.SubscriptionModified notification)
    {
        throw new NotImplementedException();
    }

    public Task OnSubscriptionResumed(Ietf.Subscribed.Notifications.YangNode.SubscriptionResumed notification)
    {
        throw new NotImplementedException();
    }

    public Task OnSubscriptionStarted(Ietf.Subscribed.Notifications.YangNode.SubscriptionStarted notification)
    {
        throw new NotImplementedException();
    }

    public Task OnSubscriptionSuspended(Ietf.Subscribed.Notifications.YangNode.SubscriptionSuspended notification)
    {
        throw new NotImplementedException();
    }

    public Task OnSubscriptionTerminated(Ietf.Subscribed.Notifications.YangNode.SubscriptionTerminated notification)
    {
        throw new NotImplementedException();
    }

    public Task OnHardwareStateOperDisabled(Ietf.Hardware.State.YangNode.HardwareStateOperDisabled notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnClearAdjacency(Ietf.Isis.YangNode.ClearAdjacencyInput input)
    {
        throw new NotImplementedException();
    }

    public Task OnClearDatabase(Ietf.Isis.YangNode.ClearDatabaseInput input)
    {
        throw new NotImplementedException();
    }

    public Task OnDatabaseOverload(Ietf.Isis.YangNode.DatabaseOverload notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnLspTooLarge(Ietf.Isis.YangNode.LspTooLarge notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnIfStateChange(Ietf.Isis.YangNode.IfStateChange notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnCorruptedLspDetected(Ietf.Isis.YangNode.CorruptedLspDetected notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnAttemptToExceedMaxSequence(Ietf.Isis.YangNode.AttemptToExceedMaxSequence notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnIdLenMismatch(Ietf.Isis.YangNode.IdLenMismatch notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnMaxAreaAddressesMismatch(Ietf.Isis.YangNode.MaxAreaAddressesMismatch notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnOwnLspPurge(Ietf.Isis.YangNode.OwnLspPurge notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnSequenceNumberSkipped(Ietf.Isis.YangNode.SequenceNumberSkipped notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnAuthenticationTypeFailure(Ietf.Isis.YangNode.AuthenticationTypeFailure notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnAuthenticationFailure(Ietf.Isis.YangNode.AuthenticationFailure notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnVersionSkew(Ietf.Isis.YangNode.VersionSkew notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnAreaMismatch(Ietf.Isis.YangNode.AreaMismatch notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnRejectedAdjacency(Ietf.Isis.YangNode.RejectedAdjacency notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnProtocolsSupportedMismatch(Ietf.Isis.YangNode.ProtocolsSupportedMismatch notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnLspErrorDetected(Ietf.Isis.YangNode.LspErrorDetected notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnAdjacencyStateChange(Ietf.Isis.YangNode.AdjacencyStateChange notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnLspReceived(Ietf.Isis.YangNode.LspReceived notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnLspGeneration(Ietf.Isis.YangNode.LspGeneration notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnDatabaseOverload(Ietf.Isis.YangNode.DatabaseOverload notification)
    {
        throw new NotImplementedException();
    }

    public Task OnLspTooLarge(Ietf.Isis.YangNode.LspTooLarge notification)
    {
        throw new NotImplementedException();
    }

    public Task OnIfStateChange(Ietf.Isis.YangNode.IfStateChange notification)
    {
        throw new NotImplementedException();
    }

    public Task OnCorruptedLspDetected(Ietf.Isis.YangNode.CorruptedLspDetected notification)
    {
        throw new NotImplementedException();
    }

    public Task OnAttemptToExceedMaxSequence(Ietf.Isis.YangNode.AttemptToExceedMaxSequence notification)
    {
        throw new NotImplementedException();
    }

    public Task OnIdLenMismatch(Ietf.Isis.YangNode.IdLenMismatch notification)
    {
        throw new NotImplementedException();
    }

    public Task OnMaxAreaAddressesMismatch(Ietf.Isis.YangNode.MaxAreaAddressesMismatch notification)
    {
        throw new NotImplementedException();
    }

    public Task OnOwnLspPurge(Ietf.Isis.YangNode.OwnLspPurge notification)
    {
        throw new NotImplementedException();
    }

    public Task OnSequenceNumberSkipped(Ietf.Isis.YangNode.SequenceNumberSkipped notification)
    {
        throw new NotImplementedException();
    }

    public Task OnAuthenticationTypeFailure(Ietf.Isis.YangNode.AuthenticationTypeFailure notification)
    {
        throw new NotImplementedException();
    }

    public Task OnAuthenticationFailure(Ietf.Isis.YangNode.AuthenticationFailure notification)
    {
        throw new NotImplementedException();
    }

    public Task OnVersionSkew(Ietf.Isis.YangNode.VersionSkew notification)
    {
        throw new NotImplementedException();
    }

    public Task OnAreaMismatch(Ietf.Isis.YangNode.AreaMismatch notification)
    {
        throw new NotImplementedException();
    }

    public Task OnRejectedAdjacency(Ietf.Isis.YangNode.RejectedAdjacency notification)
    {
        throw new NotImplementedException();
    }

    public Task OnProtocolsSupportedMismatch(Ietf.Isis.YangNode.ProtocolsSupportedMismatch notification)
    {
        throw new NotImplementedException();
    }

    public Task OnLspErrorDetected(Ietf.Isis.YangNode.LspErrorDetected notification)
    {
        throw new NotImplementedException();
    }

    public Task OnAdjacencyStateChange(Ietf.Isis.YangNode.AdjacencyStateChange notification)
    {
        throw new NotImplementedException();
    }

    public Task OnLspReceived(Ietf.Isis.YangNode.LspReceived notification)
    {
        throw new NotImplementedException();
    }

    public Task OnLspGeneration(Ietf.Isis.YangNode.LspGeneration notification)
    {
        throw new NotImplementedException();
    }

    public Task OnHardwareStateChange(Ietf.Hardware.YangNode.HardwareStateChange notification)
    {
        throw new NotImplementedException();
    }

    public Task OnHardwareStateOperEnabled(Ietf.Hardware.YangNode.HardwareStateOperEnabled notification)
    {
        throw new NotImplementedException();
    }

    public Task OnHardwareStateOperDisabled(Ietf.Hardware.YangNode.HardwareStateOperDisabled notification)
    {
        throw new NotImplementedException();
    }

    public Task OnSegmentRoutingSrgbCollision(
        Ietf.Segment.Routing.Mpls.YangNode.SegmentRoutingSrgbCollision notification)
    {
        throw new NotImplementedException();
    }

    public Task OnSegmentRoutingGlobalSidCollision(
        Ietf.Segment.Routing.Mpls.YangNode.SegmentRoutingGlobalSidCollision notification)
    {
        throw new NotImplementedException();
    }

    public Task OnSegmentRoutingIndexOutOfRange(
        Ietf.Segment.Routing.Mpls.YangNode.SegmentRoutingIndexOutOfRange notification)
    {
        throw new NotImplementedException();
    }

    public Task OnResyncSubscription(Ietf.Yang.Push.YangNode.ResyncSubscriptionInput input)
    {
        throw new NotImplementedException();
    }

    public Task OnPushUpdate(Ietf.Yang.Push.YangNode.PushUpdate notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnPushChangeUpdate(Ietf.Yang.Push.YangNode.PushChangeUpdate notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnPushUpdate(Ietf.Yang.Push.YangNode.PushUpdate notification)
    {
        throw new NotImplementedException();
    }

    public Task OnPushChangeUpdate(Ietf.Yang.Push.YangNode.PushChangeUpdate notification)
    {
        throw new NotImplementedException();
    }

    public Task OnClearPeer(
        Ietf.Routing.YangNode.RoutingContainer.ControlPlaneProtocolsContainer.ControlPlaneProtocolEntry.MsdpContainer.
            PeersContainer.PeerEntry target)
    {
        throw new NotImplementedException();
    }

    public Task OnClearAllPeers(
        Ietf.Routing.YangNode.RoutingContainer.ControlPlaneProtocolsContainer.ControlPlaneProtocolEntry.MsdpContainer
            target)
    {
        throw new NotImplementedException();
    }

    public Task OnClear(
        Ietf.Routing.YangNode.RoutingContainer.ControlPlaneProtocolsContainer.ControlPlaneProtocolEntry.MsdpContainer.
            SaCacheContainer.ClearInput input)
    {
        throw new NotImplementedException();
    }

    public Task<Ietf.Dhcpv6.Server.YangNode.DeleteAddressLeaseOutput> OnDeleteAddressLease(
        Ietf.Dhcpv6.Server.YangNode.DeleteAddressLeaseInput input)
    {
        throw new NotImplementedException();
    }

    public Task<Ietf.Dhcpv6.Server.YangNode.DeletePrefixLeaseOutput> OnDeletePrefixLease(
        Ietf.Dhcpv6.Server.YangNode.DeletePrefixLeaseInput input)
    {
        throw new NotImplementedException();
    }

    public Task OnAddressPoolUtilizationThresholdExceeded(
        Ietf.Dhcpv6.Server.YangNode.AddressPoolUtilizationThresholdExceeded notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnPrefixPoolUtilizationThresholdExceeded(Ietf.Dhcpv6.Server.YangNode.PrefixPoolUtilizationThresholdExceeded notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnInvalidClientDetected(Ietf.Dhcpv6.Server.YangNode.InvalidClientDetected notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnDeclineReceived(Ietf.Dhcpv6.Server.YangNode.DeclineReceived notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnPrefixPoolUtilizationThresholdExceeded(
        Ietf.Dhcpv6.Server.YangNode.PrefixPoolUtilizationThresholdExceeded notification)
    {
        throw new NotImplementedException();
    }

    public Task OnInvalidClientDetected(Ietf.Dhcpv6.Server.YangNode.InvalidClientDetected notification)
    {
        throw new NotImplementedException();
    }

    public Task OnDeclineReceived(Ietf.Dhcpv6.Server.YangNode.DeclineReceived notification)
    {
        throw new NotImplementedException();
    }

    public Task OnNonSuccessCodeSent(Ietf.Dhcpv6.Server.YangNode.NonSuccessCodeSent notification)
    {
        throw new NotImplementedException();
    }

    public Task OnNatPoolEvent(Ietf.Nat.YangNode.NatPoolEvent notification)
    {
        throw new NotImplementedException();
    }

    public Task OnNatInstanceEvent(Ietf.Nat.YangNode.NatInstanceEvent notification)
    {
        throw new NotImplementedException();
    }

    public Task OnClearIgmpSnoopingGroups(
        Ietf.Routing.YangNode.RoutingContainer.ControlPlaneProtocolsContainer.ControlPlaneProtocolEntry.
            IgmpSnoopingInstanceContainer.ClearIgmpSnoopingGroupsInput input)
    {
        throw new NotImplementedException();
    }

    public Task OnClearMldSnoopingGroups(
        Ietf.Routing.YangNode.RoutingContainer.ControlPlaneProtocolsContainer.ControlPlaneProtocolEntry.
            MldSnoopingInstanceContainer.ClearMldSnoopingGroupsInput input)
    {
        throw new NotImplementedException();
    }

    public Task OnPimRpEvent(Ietf.Pim.Rp.YangNode.PimRpEvent notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnPimNeighborEvent(Ietf.Pim.Base.YangNode.PimNeighborEvent notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnPimInterfaceEvent(Ietf.Pim.Base.YangNode.PimInterfaceEvent notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnBindNiNameFailed(Ietf.Network.Instance.YangNode.BindNiNameFailed notification)
    {
        throw new NotImplementedException();
    }

    public Task OnPimRpEvent(Ietf.Pim.Rp.YangNode.PimRpEvent notification)
    {
        throw new NotImplementedException();
    }

    public Task OnSadbAcquire(Ietf.I2nsf.Ikeless.YangNode.SadbAcquire notification)
    {
        throw new NotImplementedException();
    }

    public Task OnSadbExpire(Ietf.I2nsf.Ikeless.YangNode.SadbExpire notification)
    {
        throw new NotImplementedException();
    }

    public Task OnSadbSeqOverflow(Ietf.I2nsf.Ikeless.YangNode.SadbSeqOverflow notification)
    {
        throw new NotImplementedException();
    }

    public Task OnSadbBadSpi(Ietf.I2nsf.Ikeless.YangNode.SadbBadSpi notification)
    {
        throw new NotImplementedException();
    }

    public Task OnSinglehopNotification(Ietf.Bfd.Ip.Sh.YangNode.SinglehopNotification notification)
    {
        throw new NotImplementedException();
    }

    public Task OnPimNeighborEvent(Ietf.Pim.Base.YangNode.PimNeighborEvent notification)
    {
        throw new NotImplementedException();
    }

    public Task OnPimInterfaceEvent(Ietf.Pim.Base.YangNode.PimInterfaceEvent notification)
    {
        throw new NotImplementedException();
    }

    public Task OnSinglehopNotification(Ietf.Bfd.Ip.Sh.YangNode.SinglehopNotification notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task<Ietf.Netconf.Nmda.YangNode.GetDataOutput> OnGetData(Ietf.Netconf.Nmda.YangNode.GetDataInput input)
    {
        throw new NotImplementedException();
    }

    public Task OnEditData(Ietf.Netconf.Nmda.YangNode.EditDataInput input)
    {
        throw new NotImplementedException();
    }

    public Task OnHardwareStateChange(Ietf.Hardware.State.YangNode.HardwareStateChange notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnHardwareStateOperEnabled(Ietf.Hardware.State.YangNode.HardwareStateOperEnabled notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnYangLibraryUpdate(Ietf.Yang.Library.YangNode.YangLibraryUpdate notification)
    {
        throw new NotImplementedException();
    }

    public Task OnYangLibraryChange(Ietf.Yang.Library.YangNode.YangLibraryChange notification)
    {
        throw new NotImplementedException();
    }

    public Task OnPryMaxPeersExceeded(Ieee802.Dot1ae.Pry.YangNode.PryMaxPeersExceeded notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task<Ietf.Dhcpv6.Relay.YangNode.ClearPrefixEntryOutput> OnClearPrefixEntry(
        Ietf.Dhcpv6.Relay.YangNode.ClearPrefixEntryInput input)
    {
        throw new NotImplementedException();
    }

    public Task<Ietf.Dhcpv6.Relay.YangNode.ClearClientPrefixesOutput> OnClearClientPrefixes(
        Ietf.Dhcpv6.Relay.YangNode.ClearClientPrefixesInput input)
    {
        throw new NotImplementedException();
    }

    public Task<Ietf.Dhcpv6.Relay.YangNode.ClearInterfacePrefixesOutput> OnClearInterfacePrefixes(
        Ietf.Dhcpv6.Relay.YangNode.ClearInterfacePrefixesInput input)
    {
        throw new NotImplementedException();
    }

    public Task OnRelayEvent(Ietf.Dhcpv6.Relay.YangNode.RelayEvent notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnRelayEvent(Ietf.Dhcpv6.Relay.YangNode.RelayEvent notification)
    {
        throw new NotImplementedException();
    }

    public Task<Ietf.Netconf.YangNode.GetConfigOutput> OnGetConfig(Ietf.Netconf.YangNode.GetConfigInput input)
    {
        throw new NotImplementedException();
    }

    public Task OnEditConfig(Ietf.Netconf.YangNode.EditConfigInput input)
    {
        throw new NotImplementedException();
    }

    public Task OnCopyConfig(Ietf.Netconf.YangNode.CopyConfigInput input)
    {
        throw new NotImplementedException();
    }

    public Task OnDeleteConfig(Ietf.Netconf.YangNode.DeleteConfigInput input)
    {
        throw new NotImplementedException();
    }

    public Task OnLock(Ietf.Netconf.YangNode.LockInput input)
    {
        throw new NotImplementedException();
    }

    public Task OnUnlock(Ietf.Netconf.YangNode.UnlockInput input)
    {
        throw new NotImplementedException();
    }

    public Task<Ietf.Netconf.YangNode.GetOutput> OnGet(Ietf.Netconf.YangNode.GetInput input)
    {
        throw new NotImplementedException();
    }

    public Task OnCloseSession()
    {
        throw new NotImplementedException();
    }

    public Task OnKillSession(Ietf.Netconf.YangNode.KillSessionInput input)
    {
        throw new NotImplementedException();
    }

    public Task OnCommit(Ietf.Netconf.YangNode.CommitInput input)
    {
        throw new NotImplementedException();
    }

    public Task OnDiscardChanges()
    {
        throw new NotImplementedException();
    }

    public Task OnCancelCommit(Ietf.Netconf.YangNode.CancelCommitInput input)
    {
        throw new NotImplementedException();
    }

    public Task OnValidate(Ietf.Netconf.YangNode.ValidateInput input)
    {
        throw new NotImplementedException();
    }

    public Task OnVrrpNewMasterEvent(Ietf.Vrrp.YangNode.VrrpNewMasterEvent notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnVrrpProtocolErrorEvent(Ietf.Vrrp.YangNode.VrrpProtocolErrorEvent notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnVrrpVirtualRouterErrorEvent(Ietf.Vrrp.YangNode.VrrpVirtualRouterErrorEvent notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnSadbAcquire(Ietf.I2nsf.Ikeless.YangNode.SadbAcquire notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnSadbExpire(Ietf.I2nsf.Ikeless.YangNode.SadbExpire notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnSadbSeqOverflow(Ietf.I2nsf.Ikeless.YangNode.SadbSeqOverflow notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnSadbBadSpi(Ietf.I2nsf.Ikeless.YangNode.SadbBadSpi notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnYangLibraryUpdate(Ietf.Yang.Library.YangNode.YangLibraryUpdate notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnYangLibraryChange(Ietf.Yang.Library.YangNode.YangLibraryChange notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnL2NodeEvent(Ietf.L2.Topology.YangNode.L2NodeEvent notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnL2LinkEvent(Ietf.L2.Topology.YangNode.L2LinkEvent notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnL2TerminationPointEvent(Ietf.L2.Topology.YangNode.L2TerminationPointEvent notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnMultihopNotification(Ietf.Bfd.Ip.Mh.YangNode.MultihopNotification notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnLagNotification(Ietf.Bfd.Lag.YangNode.LagNotification notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnReport(Ietf.Lmap.Report.YangNode.ReportInput input)
    {
        throw new NotImplementedException();
    }

    public Task OnBindNiNameFailed(Ietf.Network.Instance.YangNode.BindNiNameFailed notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnLagNotification(Ietf.Bfd.Lag.YangNode.LagNotification notification)
    {
        throw new NotImplementedException();
    }

    public Task OnMultihopNotification(Ietf.Bfd.Ip.Mh.YangNode.MultihopNotification notification)
    {
        throw new NotImplementedException();
    }

    public Task OnVrrpNewMasterEvent(Ietf.Vrrp.YangNode.VrrpNewMasterEvent notification)
    {
        throw new NotImplementedException();
    }

    public Task OnVrrpProtocolErrorEvent(Ietf.Vrrp.YangNode.VrrpProtocolErrorEvent notification)
    {
        throw new NotImplementedException();
    }

    public Task OnVrrpVirtualRouterErrorEvent(Ietf.Vrrp.YangNode.VrrpVirtualRouterErrorEvent notification)
    {
        throw new NotImplementedException();
    }

    public Task OnHardwareStateChange(Ietf.Hardware.State.YangNode.HardwareStateChange notification)
    {
        throw new NotImplementedException();
    }

    public Task OnHardwareStateOperEnabled(Ietf.Hardware.State.YangNode.HardwareStateOperEnabled notification)
    {
        throw new NotImplementedException();
    }

    public Task OnHardwareStateOperDisabled(Ietf.Hardware.State.YangNode.HardwareStateOperDisabled notification)
    {
        throw new NotImplementedException();
    }

    public Task OnFactoryReset()
    {
        throw new NotImplementedException();
    }

    public Task OnL3NodeEvent(Ietf.L3.Unicast.Topology.YangNode.L3NodeEvent notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnL3LinkEvent(Ietf.L3.Unicast.Topology.YangNode.L3LinkEvent notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnL3PrefixEvent(Ietf.L3.Unicast.Topology.YangNode.L3PrefixEvent notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnTerminationPointEvent(Ietf.L3.Unicast.Topology.YangNode.TerminationPointEvent notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnL2NodeEvent(Ietf.L2.Topology.YangNode.L2NodeEvent notification)
    {
        throw new NotImplementedException();
    }

    public Task OnL2LinkEvent(Ietf.L2.Topology.YangNode.L2LinkEvent notification)
    {
        throw new NotImplementedException();
    }

    public Task OnL2TerminationPointEvent(Ietf.L2.Topology.YangNode.L2TerminationPointEvent notification)
    {
        throw new NotImplementedException();
    }

    public Task OnL3NodeEvent(Ietf.L3.Unicast.Topology.YangNode.L3NodeEvent notification)
    {
        throw new NotImplementedException();
    }

    public Task OnL3LinkEvent(Ietf.L3.Unicast.Topology.YangNode.L3LinkEvent notification)
    {
        throw new NotImplementedException();
    }

    public Task OnL3PrefixEvent(Ietf.L3.Unicast.Topology.YangNode.L3PrefixEvent notification)
    {
        throw new NotImplementedException();
    }

    public Task OnTerminationPointEvent(Ietf.L3.Unicast.Topology.YangNode.TerminationPointEvent notification)
    {
        throw new NotImplementedException();
    }

    public Task OnMplsLdpClearPeer(Ietf.Mpls.Ldp.YangNode.MplsLdpClearPeerInput input)
    {
        throw new NotImplementedException();
    }

    public Task OnMplsLdpClearHelloAdjacency(Ietf.Mpls.Ldp.YangNode.MplsLdpClearHelloAdjacencyInput input)
    {
        throw new NotImplementedException();
    }

    public Task OnMplsLdpClearPeerStatistics(Ietf.Mpls.Ldp.YangNode.MplsLdpClearPeerStatisticsInput input)
    {
        throw new NotImplementedException();
    }

    public Task OnMplsLdpPeerEvent(Ietf.Mpls.Ldp.YangNode.MplsLdpPeerEvent notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnMplsLdpHelloAdjacencyEvent(Ietf.Mpls.Ldp.YangNode.MplsLdpHelloAdjacencyEvent notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnMplsLdpFecEvent(Ietf.Mpls.Ldp.YangNode.MplsLdpFecEvent notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnMplsLdpPeerEvent(Ietf.Mpls.Ldp.YangNode.MplsLdpPeerEvent notification)
    {
        throw new NotImplementedException();
    }

    public Task OnMplsLdpHelloAdjacencyEvent(Ietf.Mpls.Ldp.YangNode.MplsLdpHelloAdjacencyEvent notification)
    {
        throw new NotImplementedException();
    }

    public Task OnMplsLdpFecEvent(Ietf.Mpls.Ldp.YangNode.MplsLdpFecEvent notification)
    {
        throw new NotImplementedException();
    }

    public Task OnPryMaxPeersExceeded(Ieee802.Dot1ae.Pry.YangNode.PryMaxPeersExceeded notification)
    {
        throw new NotImplementedException();
    }

    public Task OnStateChangeActionType(
        Ietf.Interfaces.YangNode.InterfacesContainer.InterfaceEntry.EthernetContainer.
            MpcpLogicalLinkAdminActionsContainer.StateChangeActionTypeInput input)
    {
        throw new NotImplementedException();
    }

    public Task OnResetActionType(
        Ietf.Interfaces.YangNode.InterfacesContainer.InterfaceEntry.EthernetContainer.
            MpcpLogicalLinkAdminActionsContainer.ResetActionTypeInput input)
    {
        throw new NotImplementedException();
    }

    public Task OnRegisterType(
        Ietf.Interfaces.YangNode.InterfacesContainer.InterfaceEntry.EthernetContainer.
            MpcpLogicalLinkAdminActionsContainer.RegisterTypeInput input)
    {
        throw new NotImplementedException();
    }

    public Task OnClearGroups(
        Ietf.Routing.YangNode.RoutingContainer.ControlPlaneProtocolsContainer.ControlPlaneProtocolEntry.IgmpContainer.
            ClearGroupsInput input)
    {
        throw new NotImplementedException();
    }

    public Task OnClearGroups(
        Ietf.Routing.YangNode.RoutingContainer.ControlPlaneProtocolsContainer.ControlPlaneProtocolEntry.MldContainer.
            ClearGroupsInput input)
    {
        throw new NotImplementedException();
    }

    public Task OnSoftwireCeEvent(Ietf.Softwire.Ce.YangNode.SoftwireCeEvent notification, DateTime eventTime)
    {
        throw new NotImplementedException();
    }

    public Task OnSoftwireCeEvent(Ietf.Softwire.Ce.YangNode.SoftwireCeEvent notification)
    {
        throw new NotImplementedException();
    }
}