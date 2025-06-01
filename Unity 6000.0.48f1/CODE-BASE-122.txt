 
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\UnityETWProvider\UnityBindingsProfile.wprp---------------


<?xml version="1.0" encoding="utf-8" standalone='yes'?>

<WindowsPerformanceRecorder Version="1.0">
  <Profiles>
 
    <!-- Event Collectors -->
    <EventCollector Id="UnityCollector" Name="Unity Collector" Private="false" ProcessPrivate="false" Secure="false" Realtime="false">
      <BufferSize Value="128"/>
      <Buffers Value="40"/>
    </EventCollector>

    <!-- ETW Event Providers -->
    <EventProvider Id="Unity" Name="F736823E-EEF1-49C0-83FB-D036F507B210">
      <Keywords>
        <Keyword Value="0x10" />
      </Keywords>
	</EventProvider>
	
    <!-- Profiles -->
    <Profile Id="UnityBindingsProfile.Verbose.File" LoggingMode="File" Name="UnityBindingsProfile" DetailLevel="Verbose" Description="Unity Bindings Profile">
      <Collectors>
        <EventCollectorId Value="UnityCollector">
          <EventProviders>
            <EventProviderId Value="Unity" />
          </EventProviders>
        </EventCollectorId>
      </Collectors>
    </Profile>
	<Profile Id="UnityBindingsProfile.Verbose.Memory" Name="UnityBindingsProfile" Description="Unity Bindings Profile" Base="UnityBindingsProfile.Verbose.File" LoggingMode="Memory" DetailLevel="Verbose" />
  </Profiles>

  <TraceMergeProperties>
    <TraceMergeProperty  Id="TraceMerge_Default" Name="TraceMerge_Default">
      <CustomEvents>
        <CustomEvent Value="ImageId"/>
        <CustomEvent Value="BuildInfo"/>
        <CustomEvent Value="VolumeMapping"/>
        <CustomEvent Value="EventMetadata"/>
        <CustomEvent Value="PerfTrackMetadata"/>
        <CustomEvent Value="WinSAT"/>
        <CustomEvent Value="NetworkInterface"/>
      </CustomEvents>
    </TraceMergeProperty>
  </TraceMergeProperties>

</WindowsPerformanceRecorder>


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\UnityETWProvider\UnityBindingsProfile.wprp---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\UnityETWProvider\UnityEventsProfile.wprp---------------


<?xml version="1.0" encoding="utf-8" standalone='yes'?>

<WindowsPerformanceRecorder Version="1.0">
  <Profiles>
 
    <!-- Event Collectors -->
    <EventCollector Id="UnityCollector" Name="Unity Collector" Private="false" ProcessPrivate="false" Secure="false" Realtime="false">
      <BufferSize Value="128"/>
      <Buffers Value="40"/>
    </EventCollector>

    <!-- ETW Event Providers -->
    <EventProvider Id="Unity" Name="F736823E-EEF1-49C0-83FB-D036F507B210">
      <Keywords>
        <Keyword Value="0xFFFFFF6F" />
      </Keywords>
	</EventProvider>
	
    <!-- Profiles -->
    <Profile Id="UnityEventsProfile.Verbose.File" LoggingMode="File" Name="UnityEventsProfile" DetailLevel="Verbose" Description="Unity Events Profile">
      <Collectors>
        <EventCollectorId Value="UnityCollector">
          <EventProviders>
            <EventProviderId Value="Unity" />
          </EventProviders>
        </EventCollectorId>
      </Collectors>
    </Profile>
	<Profile Id="UnityEventsProfile.Verbose.Memory" Name="UnityEventsProfile" Description="Unity Events Profile" Base="UnityEventsProfile.Verbose.File" LoggingMode="Memory" DetailLevel="Verbose" />
  </Profiles>

  <TraceMergeProperties>
    <TraceMergeProperty  Id="TraceMerge_Default" Name="TraceMerge_Default">
      <CustomEvents>
        <CustomEvent Value="ImageId"/>
        <CustomEvent Value="BuildInfo"/>
        <CustomEvent Value="VolumeMapping"/>
        <CustomEvent Value="EventMetadata"/>
        <CustomEvent Value="PerfTrackMetadata"/>
        <CustomEvent Value="WinSAT"/>
        <CustomEvent Value="NetworkInterface"/>
      </CustomEvents>
    </TraceMergeProperty>
  </TraceMergeProperties>

</WindowsPerformanceRecorder>


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\UnityETWProvider\UnityEventsProfile.wprp---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\UnityETWProvider\UnityProfilerProfile.wprp---------------


<?xml version="1.0" encoding="utf-8" standalone='yes'?>

<WindowsPerformanceRecorder Version="1.0">
  <Profiles>
 
    <!-- Event Collectors -->
    <EventCollector Id="UnityCollector" Name="Unity Collector" Private="false" ProcessPrivate="false" Secure="false" Realtime="false">
      <BufferSize Value="128"/>
      <Buffers Value="40"/>
    </EventCollector>

    <!-- ETW Event Providers -->
    <EventProvider Id="Unity" Name="F736823E-EEF1-49C0-83FB-D036F507B210">
      <Keywords>
        <Keyword Value="0x80" />
      </Keywords>
	</EventProvider>
	
    <!-- Profiles -->
    <Profile Id="UnityProfilerProfile.Verbose.File" LoggingMode="File" Name="UnityProfilerProfile" DetailLevel="Verbose" Description="Unity Profiler Profile">
      <Collectors>
        <EventCollectorId Value="UnityCollector">
          <EventProviders>
            <EventProviderId Value="Unity" />
          </EventProviders>
        </EventCollectorId>
      </Collectors>
    </Profile>
	<Profile Id="UnityProfilerProfile.Verbose.Memory" Name="UnityProfilerProfile" Description="Unity Profiler Profile" Base="UnityProfilerProfile.Verbose.File" LoggingMode="Memory" DetailLevel="Verbose" />
  </Profiles>

  <TraceMergeProperties>
    <TraceMergeProperty  Id="TraceMerge_Default" Name="TraceMerge_Default">
      <CustomEvents>
        <CustomEvent Value="ImageId"/>
        <CustomEvent Value="BuildInfo"/>
        <CustomEvent Value="VolumeMapping"/>
        <CustomEvent Value="EventMetadata"/>
        <CustomEvent Value="PerfTrackMetadata"/>
        <CustomEvent Value="WinSAT"/>
        <CustomEvent Value="NetworkInterface"/>
      </CustomEvents>
    </TraceMergeProperty>
  </TraceMergeProperties>

</WindowsPerformanceRecorder>


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\UnityETWProvider\UnityProfilerProfile.wprp---------------


