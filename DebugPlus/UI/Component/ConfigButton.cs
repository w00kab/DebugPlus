using DebugPlus.UI.View;
using UnityEngine;

namespace DebugPlus.UI.Component
{
    /// <summary>
    /// 实体详情界面的「修改配置」用户菜单按钮（M1 落地细节之二）。
    ///
    /// 写法完全照原版 —— Clearable.cs:12/17/133/222：
    ///   在用户菜单刷新事件里 Game.Instance.userMenu.AddButton(gameObject, button, 排序值)，
    ///   订阅用静态 EventSystem.IntraObjectHandler&lt;T&gt; 委托转发到实例方法。
    ///
    /// 为什么由 UserMenu_AppendToScreen_Patch 在运行时挂载：
    /// 事件是在实体自己的 GameObject 上触发的，所以按钮必须由实体身上的组件贡献；
    /// 而本组件是在实体已 Spawn 之后才 AddComponent 上去的，框架的生命周期回调不保证会跑，
    /// 因此订阅由 EnsureOn 直接执行，并用 subscribed 标记保证只订阅一次
    /// （万一框架也回调了 OnPrefabInit，也不会重复添加按钮）。
    ///
    /// 本组件**不做任何序列化**：它纯粹为 UI 存在，读档后由 A1 重新挂上，不影响存档。
    /// </summary>
    public class ConfigButton : KMonoBehaviour
    {
        /// <summary>按钮排序值：UserMenu.AppendToScreen 内按此值升序排列，越大越靠后。</summary>
        private const float SortOrder = 20f;

        /// <summary>
        /// GameHashes.RefreshUserMenu = 493375141（GameHashes.cs:113）。
        /// 原版 Subscribe 调用一律传 int 常量字面量（Clearable.cs:17 同款），故此处照做。
        /// </summary>
        private const int RefreshUserMenu = 493375141;

        /// <summary>原版式静态转发委托（Clearable.cs:222 同款结构）。</summary>
        private static readonly global::EventSystem.IntraObjectHandler<ConfigButton> OnRefreshUserMenuDelegate =
            new global::EventSystem.IntraObjectHandler<ConfigButton>(delegate (ConfigButton component, object data)
            {
                component.OnRefreshUserMenu(data);
            });

        private bool subscribed;

        /// <summary>确保实体身上挂着本组件且已完成订阅（可重复调用）。</summary>
        public static void EnsureOn(GameObject go)
        {
            if (go == null)
            {
                return;
            }
            ConfigButton button = go.GetComponent<ConfigButton>();
            if (button == null)
            {
                button = go.AddComponent<ConfigButton>();
            }
            // Awake 只在激活的 GameObject 上立即触发；若父级此刻非激活，这里补齐框架初始化
            // （InitializeComponent 由 isInitialized 守卫，已初始化时立即返回；Subscribe 依赖它赋值的 obj）。
            button.InitializeComponent();
            button.SubscribeOnce();
        }

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            SubscribeOnce();
        }

        private void SubscribeOnce()
        {
            if (subscribed)
            {
                return;
            }
            subscribed = true;
            Subscribe<ConfigButton>(RefreshUserMenu, OnRefreshUserMenuDelegate);
        }

        private void OnRefreshUserMenu(object data)
        {
            if (Game.Instance == null)
            {
                return;
            }
            var button = new KIconButtonMenu.ButtonInfo(
                "action_switch_toggle",
                STRINGS.UI.DEBUGPLUS.CONFIG_BUTTON,
                new System.Action(OnClick),
                global::Action.NumActions,
                null,
                null,
                null,
                STRINGS.UI.DEBUGPLUS.CONFIG_BUTTON_TOOLTIP,
                true);
            Game.Instance.userMenu.AddButton(gameObject, button, SortOrder);
        }

        private void OnClick()
        {
            ConfigPanel.OpenFor(gameObject);
        }
    }
}
