namespace Newbe.BetterModLoader;

/// <summary>
/// Localization key constants for BetterModLoader (bml_ prefixed)
/// </summary>
[LanguageSupport("en", "zh-hant", "zh", "fr", "de", "es", "ru", "ja", "ko", "pt")]
public static class LK
{
    internal static class UI
    {
        public const string ApplyEnableAll = "bml_ui_mod_button_enable_all";
        public const string ApplyDisableAll = "bml_ui_mod_button_disable_all";
        public const string Back = "bml_ui_back";
        public const string ApplyChanges = "bml_ui_apply_changes";
        public const string CancelAndExit = "bml_ui_cancel_and_exit";
        public const string DangerIcon = "bml_ui_danger_icon";
        public const string Title = "bml_ui_title";
        public const string GroupList = "bml_ui_group_list";
        public const string ModList = "bml_ui_mod_list";
        public const string ToggleOn = "bml_ui_toggle_on";
        public const string ToggleOff = "bml_ui_toggle_off";
        public const string UnsavedChangesPrompt = "bml_ui_unsaved_changes_prompt";
        public const string ModNoDescription = "bml_ui_mod_no_description";
        public const string GroupFooterAddButton = "bml_ui_group_footer_add_button";
        public const string GroupFooterPlaceholder = "bml_ui_group_footer_placeholder";
        public const string GroupListTopHeader = "bml_ui_group_list_top_header";
        public const string GroupListOtherHeader = "bml_ui_group_list_other_header";
        public const string GroupLockedAddRemoveWarning = "bml_ui_group_locked_add_remove_warning";
        public const string GroupRenameExists = "bml_ui_group_rename_exists";
        public const string GroupRenameEmpty = "bml_ui_group_rename_empty";
        public const string GroupRenameNotAllowed = "bml_ui_group_rename_not_allowed";
        public const string GroupRenameCompleteCurrentWarning = "bml_ui_group_rename_complete_current_warning";
        public const string GroupRenameCompleteCurrent = "bml_ui_group_rename_complete_current";
        public const string GroupItem = "bml_ui_group_item";
        public const string ModItem = "bml_ui_mod_item";
        public const string DeveloperInfo = "bml_ui_developer_info";
        public const string DeveloperInfoLocalModsFolder = "bml_ui_developerinfo_local_mods_folder";
        public const string DeveloperInfoSteamModsFolder = "bml_ui_developerinfo_steam_mods_folder";
        public const string UploadReady = "bml_ui_upload_ready";
        public const string UploadChecklistNotReady = "bml_ui_upload_checklist_not_ready";
        public const string Uploading = "bml_ui_uploading";

        public const string OriginalGameLogsFolder = "bml_ui_developerinfo_original_game_logs_folder";

        public const string DeveloperInfoGamePersistentDataFolder =
            "bml_ui_developerinfo_game_persistent_data_folder";

        public const string DeveloperInfoButtonCopy = "bml_ui_developerinfo_button_copy";
        public const string DeveloperInfoButtonOpen = "bml_ui_developerinfo_button_open";
        public const string ForceRestartToggle = "bml_ui_developerinfo_force_restart_toggle";
        public const string BmlConfigFolder = "bml_ui_developerinfo_bml_config_folder";
        public const string BmlConfigPath = "bml_ui_developerinfo_bml_config_path";
        public const string ModOptionsForAllFolder = "bml_ui_developerinfo_mod_options_for_all_folder";
        public const string ModOptionsForEachFolder = "bml_ui_developerinfo_mod_options_for_each_folder";

        public const string RollingTextAutoUpdateModAvailable =
            "bml_ui_rolling_text_auto_update_mod_available";

        public const string RollingTextNewUpdatedAvailable = "bml_ui_rolling_text_new_updated_available";

        public const string RollingTextLearnToDevMod = "bml_ui_rolling_text_learn_to_dev_mod";

        public const string RollingTextSubscribeLogEfficient = "bml_ui_rolling_text_subscribe_log_efficient";

        public const string RollingTextSubscribeFpsMonitor = "bml_ui_rolling_text_subscribe_fps_monitor";
    }

    internal static class Group
    {
        public const string FooterAddButton = "bml_ui_group_footer_add_button";
        public const string FooterPlaceholder = "bml_ui_group_footer_placeholder";
        public const string ListTopHeader = "bml_ui_group_list_top_header";
        public const string ListOtherHeader = "bml_ui_group_list_other_header";
        public const string LockedAddRemoveWarning = "bml_ui_group_locked_add_remove_warning";
        public const string RenameExists = "bml_ui_group_rename_exists";
        public const string RenameEmpty = "bml_ui_group_rename_empty";
        public const string RenameNotAllowed = "bml_ui_group_rename_not_allowed";
        public const string RenameCompleteCurrentWarning = "bml_ui_group_rename_complete_current_warning";
        public const string RenameCompleteCurrent = "bml_ui_group_rename_complete_current";
        public const string Item = "bml_ui_group_item";
        public const string DeleteGroupButton = "bml_ui_group_delete_group_button";
    }

    internal static class Mod
    {
        public const string LocalModListTitle = "bml_ui_mod_local_mod_list_title";
        public const string ButtonFolder = "bml_ui_mod_button_folder";
        public const string ButtonSteam = "bml_ui_mod_button_steam";
        public const string ButtonUpload = "bml_ui_mod_button_upload";
        public const string BadgeSteam = "bml_ui_mod_badge_steam";
        public const string LabelId = "bml_ui_mod_label_id";

        public const string LabelSteamId = "bml_ui_mod_label_steam_id";

        // Labels for detail fields (localized)
        public const string LabelDisplayName = "bml_ui_mod_label_display_name";
        public const string LabelName = "bml_ui_mod_label_name";
        public const string LabelDescription = "bml_ui_mod_label_description";
        public const string LabelTags = "bml_ui_mod_label_tags";
        public const string LabelDependencies = "bml_ui_mod_label_dependencies";
        public const string LabelBbCodeDescription = "bml_ui_mod_label_bbcode_description";
        public const string TitleDeveloperMode = "bml_ui_mod_title_developer_mode";
        public const string ButtonDeveloperMode = "bml_ui_mod_button_developer_mode";
        public const string NoDescription = "bml_ui_mod_no_description";

        public const string ButtonDanger = "bml_mod_button_danger";
        public const string ButtonBack = "bml_mod_button_back";
        public const string ButtonApplyChanges = "bml_mod_button_apply_changes";
        public const string ButtonCancelAndExit = "bml_mod_button_cancel_and_exit";

        public const string LabelVersion = "bml_ui_mod_label_version";


        // Misc
        public const string Item = "bml_ui_mod_item";

        // Tooltips and sorts
        public const string TooltipLockedToggle = "bml_ui_tooltip_group_locked_toggle";

        public const string TooltipLockedSort = "bml_ui_tooltip_group_locked_sort";

        // Enable/Disable All
        public const string ButtonEnableAll = "bml_ui_mod_button_enable_all";
        public const string ButtonDisableAll = "bml_ui_mod_button_disable_all";
    }

    internal static class Tooltip
    {
        public const string GroupLockedToggle = "bml_ui_tooltip_group_locked_toggle";
        public const string GroupLockedSort = "bml_ui_tooltip_group_locked_sort";
    }

    internal static class Migration
    {
        public const string Title = "bml_migration_title";
        public const string ButtonConfirm = "bml_migration_button_confirm";
        public const string ButtonCancel = "bml_migration_button_cancel";
        public const string Success = "bml_migration_success";
        public const string FailurePrefix = "bml_migration_failure_prefix";
        public const string BodyFull = "bml_migration_body_full";
    }

    internal static class Rollback
    {
        public const string Title = "bml_rollback_title";
        public const string ButtonConfirm = "bml_rollback_button_confirm";
        public const string ButtonCancel = "bml_rollback_button_cancel";
        public const string Success = "bml_rollback_success";
        public const string BodyFull = "bml_rollback_body_full";
    }

    internal static class ModActivation
    {
        public const string WelcomeMessage = "bml_mod_activation_welcome_message";
        public const string SuccessMessage = "bml_mod_activation_success_message";
        public const string Header = "bml_mod_activation_header";
        public const string Close = "bml_mod_activation_close";
        public const string OpenLogs = "bml_mod_activation_open_logs";
        public const string FailureHeader = "bml_mod_activation_failure_header";
        public const string RestartRequired = "bml_mod_activation_restart_required";
        public const string NoJobs = "bml_mod_activation_no_jobs";
        public const string FailurePrefix = "bml_mod_activation_failure_prefix";
        public const string DisableFailurePrefix = "bml_mod_activation_disable_failure_prefix";
        public const string ReasonNotActive = "bml_mod_activation_reason_not_active";
        public const string ReasonException = "bml_mod_activation_reason_exception";
        public const string ReasonUnknown = "bml_mod_activation_reason_unknown";
        public const string ReasonFailurePrefix = "bml_mod_activation_reason_failure_prefix";
        public const string ReasonDisableFailurePrefix = "bml_mod_activation_reason_disable_failure_prefix";
        public const string Loading = "bml_mod_activation_loading";
        public const string StatsFastestHeader = "bml_mod_activation_stats_fastest_header";
        public const string StatsSlowestHeader = "bml_mod_activation_stats_slowest_header";
        public const string StatsBestLabel = "bml_mod_activation_stats_best_label";
    }

    internal static class GroupName
    {
        public const string Default = "bml_group_name_default";
        public const string BetterModLoader = "bml_group_name_better_mod_loader";
        public const string EnabledBefore = "bml_group_name_enabled_before";
    }

    internal static class DeveloperInfo
    {
        public const string Title = "bml_developer_info_title";
        public const string EnableBmlLogs = "bml_developer_info_enable_bml_logs";
        public const string OpenFolder = "bml_developer_info_open_folder";
        public const string CopyPath = "bml_developer_info_copy_path";
    }

    internal static class ModDataCheck
    {
        public const string Title = "bml_mod_data_check_title";
        public const string ButtonCancel = "bml_mod_data_check_button_cancel";
        public const string ButtonForceApply = "bml_mod_data_check_button_force_apply";
        public const string LabelSubscribeAutoUpdate = "bml_mod_data_check_label_subscribe_auto_update";
        public const string ButtonSubscribeAutoUpdate = "bml_mod_data_check_button_subscribe_auto_update";
        public const string LabelLearnToDevMod = "bml_mod_data_check_label_learn_to_dev_mod";
        public const string ButtonLearnToDevMod = "bml_mod_data_check_button_learn_to_dev_mod";

        // Row item texts and buttons
        public const string NeedToUpdate = "bml_mod_data_check_need_to_update";
        public const string Unknown = "bml_mod_data_check_unknown";
        public const string ButtonViewPage = "bml_mod_data_check_button_view_page";
        public const string MissingDependency = "bml_mod_data_check_missing_dependency";
        public const string ButtonViewMod = "bml_mod_data_check_button_view_mod";
        public const string ButtonViewDependency = "bml_mod_data_check_button_view_dependency";
        public const string OrderError = "bml_mod_data_check_order_error";
        public const string ButtonBack = "bml_mod_data_check_button_back";
        public const string LabelResubscribeTip = "bml_mod_data_check_label_resubscribe_tip";

        // New UI element labels for enhanced ignore system
        public const string TitleOperationArea = "bml_mod_data_check_title_operation_area";
        public const string TitleActiveItems = "bml_mod_data_check_title_active_items";
        public const string TitleIgnoredItems = "bml_mod_data_check_title_ignored_items";
        public const string ButtonIgnoreAll = "bml_mod_data_check_button_ignore_all";
        public const string ButtonRestoreAll = "bml_mod_data_check_button_restore_all";
        public const string ButtonIgnore = "bml_mod_data_check_button_ignore";
        public const string ButtonRestore = "bml_data_check_button_restore";


        // Item descriptions
        public const string DescriptionNeedUpdate = "bml_mod_data_check_description_need_update";
        public const string DescriptionMissingDependency = "bml_mod_data_check_description_missing_dependency";
        public const string DescriptionModOrderError = "bml_mod_data_check_description_mod_order_error";

        public const string DescriptionCustomThingsDependency =
            "bml_mod_data_check_description_customthings_dependency";

        public const string DescriptionUnknown = "bml_mod_data_check_description_unknown";
    }

    /// <summary>
    /// Localization keys for upload panel (bml_upload_*)
    /// </summary>
    internal static class Upload
    {
        // 面板标题和按钮
        public const string PanelTitle = "bml_upload_panel_title";
        public const string ButtonBack = "bml_upload_button_back";
        public const string ButtonUpload = "bml_upload_button_upload";

        public const string ButtonUploadDisabled = "bml_upload_button_upload_disabled";

        // 表单标签
        public const string LabelChangeNotes = "bml_upload_label_change_notes";
        public const string LabelChangeNotesPlaceholder = "bml_upload_label_change_notes_placeholder";
        public const string LabelAllowBmlFooter = "bml_upload_label_allow_bml_footer";
        public const string LabelUpdateSteamDesc = "bml_upload_label_update_steam_desc";

        // 进度相关
        public const string ProgressTitle = "bml_upload_progress_title";
        public const string ProgressPreparing = "bml_upload_progress_preparing";
        public const string ProgressRequestingId = "bml_upload_progress_requesting_id";
        public const string ProgressWritingIni = "bml_upload_progress_writing_ini";
        public const string ProgressStartingSteamUpload = "bml_upload_progress_starting_steam_upload";
        public const string ProgressUploadingContent = "bml_upload_progress_uploading_content";
        public const string ProgressUploadingTitles = "bml_upload_progress_uploading_titles";
        public const string ProgressUploadingTranslations = "bml_upload_progress_uploading_translations";
        public const string ProgressCompleted = "bml_upload_progress_completed";
        public const string ProgressFailed = "bml_upload_progress_failed";
        public const string ProgressSteamNotInitialized = "bml_upload_progress_steam_not_initialized";
        public const string ProgressFailedToProcessMod = "bml_upload_progress_failed_to_process_mod";
        public const string ProgressModDllNotFound = "bml_upload_progress_mod_dll_not_found";
        public const string ProgressFailedToRequestId = "bml_upload_progress_failed_to_request_id";
        public const string ProgressFailedToWriteInfo = "bml_upload_progress_failed_to_write_info";
        public const string ProgressUploadTimeout = "bml_upload_progress_upload_timeout";
        public const string ProgressFailedToUpload = "bml_upload_progress_failed_to_upload";

        // 检查清单
        public const string ChecklistTitle = "bml_upload_checklist_title";
        public const string CheckChangeNotes = "bml_upload_check_change_notes";
        public const string CheckModInfo = "bml_upload_check_mod_info";
        public const string CheckPreview = "bml_upload_check_preview";

        public const string CheckDependencies = "bml_upload_check_dependencies";
        public const string CheckFailPrefix = "bml_upload_check_fail_prefix";

        // 检查状态描述
        public const string CheckStatusSuccess = "bml_upload_check_status_success";
        public const string CheckStatusFailed = "bml_upload_check_status_failed";
        public const string CheckStatusChecking = "bml_upload_check_status_checking";
        public const string CheckStatusUnavailable = "bml_upload_check_status_unavailable";

        public const string CheckListIniCheckDescription = "bml_upload_checklist_init_check_description";
        public const string CheckListIniCheckFailReason = "bml_upload_checklist_init_check_fail_reason";
        public const string CheckListSteamIdCheckDescription = "bml_upload_checklist_steamid_check_description";
        public const string CheckListSteamIdCheckFailReason = "bml_upload_checklist_steamid_check_fail_reason";


        public const string CheckListPreviewImageCheckDescription =
            "bml_upload_checklist_preview_image_check_description";

        public const string CheckListPreviewImageCheckFailReason =
            "bml_upload_checklist_preview_image_check_fail_reason";

        public const string CheckListPreviewImageSizeCheckDescription =
            "bml_upload_checklist_preview_image_size_check_description";

        public const string CheckListPreviewImageSizeCheckFailReason =
            "bml_upload_checklist_preview_image_size_check_fail_reason";

        public const string CheckListMultiLanguageDescriptionCheckDescription =
            "bml_upload_checklist_multilang_description_check_description";

        public const string CheckListMultiLanguageDescriptionCheckSuccess =
            "bml_upload_checklist_multilang_description_check_success";

        public const string CheckListMultiLanguageDescriptionCheckFailReason =
            "bml_upload_checklist_multilang_description_check_fail_reason";

        public const string CheckListMultiLanguageTitleCheckDescription =
            "bml_upload_checklist_multilang_title_check_description";

        public const string CheckListMultiLanguageTitleCheckSuccess =
            "bml_upload_checklist_multilang_title_check_success";

        public const string CheckListMultiLanguageTitleCheckFailReason =
            "bml_upload_checklist_multilang_title_check_fail_reason";

        public const string CheckListDescriptionSizeCheckDescription =
            "bml_upload_checklist_description_size_check_description";

        public const string CheckListDescriptionSizeCheckFailReason =
            "bml_upload_checklist_description_size_check_fail_reason";

        public const string CheckListVersionCheckDescription =
            "bml_upload_checklist_version_check_description";

        public const string CheckListVersionCheckFailReason
            = "bml_upload_checklist_version_check_fail_reason";

        // Steam Workshop Tags
        public const string TagEquipmentAndGear = "bml_upload_tag_equipment_and_gear";
        public const string TagLootAndEconomy = "bml_upload_tag_loot_and_economy";
        public const string TagQualityOfLife = "bml_upload_tag_quality_of_life";
        public const string TagCheatsAndExploits = "bml_upload_tag_cheats_and_exploits";
        public const string TagVisualEnhancements = "bml_upload_tag_visual_enhancements";
        public const string TagSound = "bml_upload_tag_sound";
        public const string TagQuestAndProgression = "bml_upload_tag_quest_and_progression";
        public const string TagCompanionAndNPC = "bml_upload_tag_companion_and_npc";
        public const string TagCollectibles = "bml_upload_tag_collectibles";
        public const string TagGameplay = "bml_upload_tag_gameplay";
        public const string TagMultiplayerAndCoOp = "bml_upload_tag_multiplayer_and_coop";
        public const string TagUtility = "bml_upload_tag_utility";
        public const string TagMedicalAndSurvival = "bml_upload_tag_medical_and_survival";
        public const string TagWeapon = "bml_upload_tag_weapon";
    }

    /// <summary>
    /// Localization keys for Healthy Gaming Advice Panel (bml_healthy_gaming_*)
    /// </summary>
    internal static class HealthyGaming
    {
        public const string Title = "bml_healthy_gaming_title";
        public const string LoadingText = "bml_healthy_gaming_loading_text";
    }
}
