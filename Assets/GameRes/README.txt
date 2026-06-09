GameRes —— YooAsset 收集根目录
================================

本目录下的所有资源由 YooAsset 收集打包，运行时通过 ResourceManager 按「地址」加载：

    await ResourceManager.Instance.LoadAsync<Sprite>(ResAddress.UI_Icons_heart);

地址规则（AddressByGameResPath）= 资源相对本目录的路径、去掉扩展名。
例：Assets/GameRes/Art/UI/Icons/heart.png  ->  地址 "Art/UI/Icons/heart"

地址不要手打，用生成的强类型常量 ResAddress.Xxx（资源增删改时自动重生成，
也可手动跑菜单 Tools/Awen/生成资源地址常量）。

收集器包名：DefaultPackage（与 ResourceManager.PackageName 一致）。

注：此 README 同时作为占位资源，保证空目录时 YooAsset 也能成功构建；
等真实资源加入后可删除。
