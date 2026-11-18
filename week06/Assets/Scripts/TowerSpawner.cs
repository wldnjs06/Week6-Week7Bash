using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
public class TowerSpawner : MonoBehaviour
{
    [SerializeField]
	private	TowerTemplateSO[] towerTemplate;	
	[SerializeField]
	private	PlayerGold playerGold;
    [SerializeField]
	private	EnemySpawner enemySpawner;	// 현재 맵에 존재하는 적 리스트 정보를 얻기 위해
    [SerializeField]
	private	SystemTextViewer systemTextViewer;
	[SerializeField]
	private	ObjectDetector objectDetector;

    private	bool isOnTowerButton = false;	
	private	GameObject followTowerClone = null;
	private	int	towerType;	

    public void ReadyToSpawnTower(int type)
	{
		towerType = type;

		if ( isOnTowerButton == true ) return;

		if ( towerTemplate[towerType].weapon[0].cost > playerGold.CurrentGold )
		{
			
			systemTextViewer.PrintText(MESSAGE.MONEY);
			return;
		}

		followTowerClone = Instantiate(towerTemplate[towerType].followTowerPrefab);
		followTowerClone.GetComponent<ObjectFollowMousePosition>().Setup(objectDetector);

		isOnTowerButton = true;

		StartCoroutine(nameof(OnTowerCancelProcess));
    }

    public void TrySpawnTower(Transform tileTransform)
    {
        if ( isOnTowerButton == false ) return;

		// 타워가 건설되어 있음으로 설정
		Tile tile = tileTransform.GetComponent<Tile>();


        if ( tile.IsbuildTower == true )
		{	
			systemTextViewer.PrintText(MESSAGE.BUILD);
			return;
		}

        isOnTowerButton = false;

		tile.IsbuildTower = true;

		playerGold.CurrentGold -= towerTemplate[towerType].weapon[0].cost;

		Vector3 position = tileTransform.position + Vector3.back;
		var clone = Instantiate(towerTemplate[towerType].towerPrefab, position, Quaternion.identity);

		clone.GetComponent<TowerWeapon>().Setup(this, enemySpawner, playerGold, tile);

		OnBuffAllBuffTowers();

		Destroy(followTowerClone);

		StopCoroutine(nameof(OnTowerCancelProcess));
    }

	private IEnumerator OnTowerCancelProcess()
	{
		while ( true )
		{
			if ( Keyboard.current.escapeKey.wasPressedThisFrame ||
				Mouse.current.rightButton.wasPressedThisFrame )
			{
				isOnTowerButton = false;
				Destroy(followTowerClone);
				break;
			}

			yield return null;
		}
	}

	public void OnBuffAllBuffTowers()
	{
		GameObject[] towers = GameObject.FindGameObjectsWithTag("Tower");

		for ( int i = 0; i < towers.Length; ++ i )
		{
			TowerWeapon weapon = towers[i].GetComponent<TowerWeapon>();

			if ( weapon.WeaponType == WeaponType.Buff )
			{
				weapon.OnBuffAroundTower();
			}
		}
	}

	public void ResetBuff() => StartCoroutine(TryResetBuff());

	private IEnumerator TryResetBuff()
	{
		yield return new WaitForEndOfFrame();

		GameObject[] towers = GameObject.FindGameObjectsWithTag("Tower");

		for ( int i = 0; i < towers.Length; ++ i )
		{
			towers[i].GetComponent<TowerWeapon>().BuffLevel = 0;
			towers[i].GetComponent<TowerWeapon>().AddedDamage = 0;
		}
		OnBuffAllBuffTowers();
	}

}
