using UnityEngine;
using System.Collections;

public enum WeaponType	{ Cannon = 0, Laser,}
public enum WeaponState { SearchClosestTarget = 0, TryAttackCannon, TryAttackLaser }

public class TowerWeapon : MonoBehaviour
{
	[Header("Commons")]
	[SerializeField]
	private	TowerTemplateSO	towerTemplate; // 타워 정보
	[SerializeField]
	private	Transform spawnPoint;
	[SerializeField]
	private	WeaponType weaponType;	
	
	[Header("Cannon")]			
	[SerializeField]
	private	GameObject projectilePrefab;		// 발사체 프리팹
	
	[Header("Laser")]
	[SerializeField]
	private	LineRenderer lineRenderer;
	[SerializeField]
	private	Transform hitEffect;				
	[SerializeField]
	private	LayerMask targetLayer;
	private	EnemySpawner enemySpawner;			// 게임에 존재하는 적 정보 획득용
	private	SpriteRenderer	spriteRenderer;	
	private	PlayerGold playerGold;	
	private	Transform attackTarget = null;	// 공격 대상
	private	Tile ownerTile;	
	private	WeaponState weaponState;			// 타워 무기의 상태
	private	int level;

	public Sprite TowerSprite => towerTemplate.weapon[level].sprite;
	public float Damage => towerTemplate.weapon[level].damage;
	public float Rate => towerTemplate.weapon[level].rate;
	public float Range => towerTemplate.weapon[level].range;
	public int Level => level + 1;
	public int MaxLevel => towerTemplate.weapon.Length;

	public void Setup(EnemySpawner enemySpawner, PlayerGold playerGold, Tile ownerTile)
	{
		spriteRenderer = GetComponentInChildren<SpriteRenderer>();
		
		this.enemySpawner = enemySpawner;
		this.playerGold	= playerGold;
		this.ownerTile	= ownerTile;

		if ( weaponType == WeaponType.Laser ) DisableLaser();
		
		// 첫 상태는 목표를 탐색하는 SearchClosestTarget으로 설정
		ChangeState(WeaponState.SearchClosestTarget);
	}

	public void ChangeState(WeaponState newState)
	{
		// 이전에 재생중이던 상태 종료
		StopCoroutine(weaponState.ToString());
		// 상태 변경
		weaponState = newState;
		// 새로운 상태 재생
		StartCoroutine(weaponState.ToString());
	}

	private void Update()
	{
		if ( attackTarget != null )
		{
			RotateToTarget();
		}
	}
	
	private void RotateToTarget()
	{
		// 원점으로부터의 거리와 수평축으로부터의 각도를 이용해 위치를 구하는 극 좌표계 이용
		// 각도 = arctan(y/x)
		// x, y 변위값 구하기
		float dx = attackTarget.position.x - transform.position.x;
		float dy = attackTarget.position.y - transform.position.y;
		// x, y 변위값을 바탕으로 각도 구하기
		// 각도가 radian 단위이기 때문에 Mathf.Rad2Deg를 곱해 도 단위를 구함
		float degree = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
		transform.rotation = Quaternion.Euler(0, 0, degree);
	}

	private IEnumerator SearchClosestTarget()
	{
		while ( true )
		{
			attackTarget = FindClosestTarge();

			if ( attackTarget != null )
			{
				if ( weaponType == WeaponType.Cannon )
				{
					ChangeState(WeaponState.TryAttackCannon);
				}
				else if ( weaponType == WeaponType.Laser )
				{
					ChangeState(WeaponState.TryAttackLaser);
				}
			}

			yield return null;
		}
	}

	private IEnumerator TryAttackCannon()
	
	
	{
		while ( true )
		{
			if (IsPossibleToAttackTarget() == false)
			{
				ChangeState(WeaponState.SearchClosestTarget);
				break;
			}
			
			yield return new WaitForSeconds(towerTemplate.weapon[level].rate);

			SpawnProjectile();
		}
	}
	private IEnumerator TryAttackLaser()
	{
		
		EnableLaser();

		while ( true )
		{			
			if ( IsPossibleToAttackTarget() == false )
			{
				DisableLaser();
				ChangeState(WeaponState.SearchClosestTarget);
				break;
			}

			SpawnLaser();

			yield return null;
		}
	}
	private void SpawnProjectile()
	{
		var clone = Instantiate(projectilePrefab, spawnPoint.position, Quaternion.identity);
		// 생성된 발사체에게 공격 대상(attackTarget) 전달
		clone.GetComponent<Projectile>().Setup(attackTarget,  towerTemplate.weapon[level].damage);
	}
	public bool Upgrade()
	{
		if ( playerGold.CurrentGold < towerTemplate.weapon[level+1].cost )
		{
			return false;
		}

		level ++;

		spriteRenderer.sprite = TowerSprite;

		playerGold.CurrentGold -= towerTemplate.weapon[level].cost;

		if ( weaponType == WeaponType.Laser )
		{		
			lineRenderer.startWidth	= 0.05f + level * 0.05f;
			lineRenderer.endWidth	= 0.05f;
		}
		
		return true;
	}

	public void Sell()
	{
		playerGold.CurrentGold += towerTemplate.weapon[level].sell;
		
		ownerTile.IsbuildTower = false;

		Destroy(gameObject);
	}
	private Transform FindClosestTarge()
	{
		
		float closestDistSqr = Mathf.Infinity;

		
		for ( int i = 0; i < enemySpawner.EnemyList.Count; ++ i )
		{
			float distance = Vector3.Distance(enemySpawner.EnemyList[i].transform.position, transform.position);
			
			if ( distance <= towerTemplate.weapon[level].range && distance <= closestDistSqr )
			{
				closestDistSqr	= distance;
				attackTarget	= enemySpawner.EnemyList[i].transform;
			}
		}

		return attackTarget;
	}

	private bool IsPossibleToAttackTarget()
	{
		
		if ( attackTarget == null )
		{
			return false;
		}

		float distance = Vector3.Distance(attackTarget.position, transform.position);
		if ( distance > towerTemplate.weapon[level].range )
		{
			attackTarget = null;

			return false;
		}

		return true;
	}

	private void EnableLaser()
	{
		lineRenderer.gameObject.SetActive(true);
		hitEffect.gameObject.SetActive(true);
	}

		private void DisableLaser()
	{
		lineRenderer.gameObject.SetActive(false);
		hitEffect.gameObject.SetActive(false);
	}

	private void SpawnLaser()
	{
		Vector3 direction = (attackTarget.position - spawnPoint.position).normalized;
		var hit = Physics2D.RaycastAll(spawnPoint.position, direction, towerTemplate.weapon[level].range, targetLayer);

		for ( int i = 0; i < hit.Length; ++ i )
		{
			if ( hit[i].transform == attackTarget )
			{
				lineRenderer.SetPosition(0, spawnPoint.position);
				lineRenderer.SetPosition(1, new Vector3(hit[i].point.x, hit[i].point.y, 0) + Vector3.back);
				
				hitEffect.position = hit[i].point;
				
				//float damage = towerTemplate.weapon[level].damage + AddedDamage;
				
				attackTarget.GetComponent<EnemyHP>().TakeDamage(towerTemplate.weapon[level].damage * Time.deltaTime);
			}
		}
	}

}

