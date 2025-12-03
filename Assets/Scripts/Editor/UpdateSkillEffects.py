import json
import re
import os

file_path = 'c:/Project/Arcana/Assets/Resources/Table/SkillList.json'

def parse_description_to_effects(description, skill_type):
    effects = []
    
    # 1. 공격 (Damage)
    # "적 하나에게 공격한다", "적 한 열에게 공격한다", "전체에게 공격한다"
    # "위력 150으로 공격"
    if "공격" in description or "데미지" in description:
        damage_val = 100 # 기본값
        
        # 위력 파싱
        power_match = re.search(r'위력\s*(\d+)', description)
        if power_match:
            damage_val = int(power_match.group(1))
            
        # 데미지 타입 (마법/물리) - 설명에 명시되지 않으면 기본 물리, 클래스나 스킬명으로 추론해야 하나 여기선 일단 물리로 통일하고 예외 처리
        damage_type = "physical"
        if "마법" in description:
            damage_type = "magical"
            
        effects.append({
            "type": "damage",
            "value": damage_val,
            "damageType": damage_type
        })

    # 2. 회복 (Heal)
    # "HP를 25%회복한다", "HP 50 회복"
    if "회복" in description:
        heal_val = 50 # 기본값
        heal_match = re.search(r'HP를?\s*(\d+)', description)
        if heal_match:
            heal_val = int(heal_match.group(1))
            
        target = "self" # 기본값
        if "아군" in description:
            target = "ally"
            
        effects.append({
            "type": "heal",
            "value": heal_val,
            "target": target
        })

    # 3. 버프 (Buff)
    # "물리 방어력+20%", "공격력+10%"
    buff_keywords = {
        "물리 방어력": "physical_defense",
        "마법 방어력": "magical_defense",
        "방어력": "physical_defense", # 통칭
        "물리 공격력": "physical_attack",
        "마법 공격력": "magical_attack",
        "공격력": "physical_attack", # 통칭
        "회피": "evasion",
        "명중": "accuracy",
        "치명타": "critical_rate",
        "가드율": "guard_rate",
        "속도": "action_speed"
    }
    
    for key, stat in buff_keywords.items():
        if f"{key}+" in description or f"{key} 상승" in description:
            # 중복 체크
            already_exists = False
            for e in effects:
                if e['type'] == 'buff' and e.get('stat') == stat:
                    already_exists = True
                    break
            
            if already_exists:
                continue

            val = 20 # 기본값
            val_match = re.search(rf'{key}\+?(\d+)', description)
            if val_match:
                val = int(val_match.group(1))
                
            effects.append({
                "type": "buff",
                "stat": stat,
                "value": val,
                "duration": 3 # 기본 3턴
            })

    # 4. 디버프 (Debuff)
    for key, stat in buff_keywords.items():
        if f"{key}-" in description or f"{key} 감소" in description:
            val = 20 # 기본값
            val_match = re.search(rf'{key}\-?(\d+)', description)
            if val_match:
                val = int(val_match.group(1))
                
            effects.append({
                "type": "debuff",
                "stat": stat,
                "value": val,
                "duration": 3
            })
            
    # 5. 상태이상 (Status)
    status_keywords = {
        "기절": "stun",
        "독": "poison",
        "화상": "burn",
        "동결": "freeze",
        "암흑": "blind",
        "가드 봉인": "guard_seal",
        "패시브 봉인": "passive_seal"
    }
    
    for key, status in status_keywords.items():
        if key in description:
            effects.append({
                "type": "status",
                "statusName": status,
                "chance": 100 # 기본 100%
            })

    # 만약 아무 효과도 파싱되지 않았는데 '공격' 스킬이라면 기본 데미지 추가
    if not effects and skill_type == "active" and "공격" in description:
         effects.append({
            "type": "damage",
            "value": 100,
            "damageType": "physical"
        })

    return effects

def process_skill_list():
    with open(file_path, 'r', encoding='utf-8') as f:
        data = json.load(f)

    updated_count = 0
    
    for class_name, skills in data.items():
        for skill in skills:
            # 기존 effects 백업 (혹시 모르니)
            # skill['original_effects'] = skill.get('effects', [])
            
            # 설명 기반으로 새로운 effects 생성
            new_effects = parse_description_to_effects(skill.get('description', ''), skill.get('type', 'active'))
            
            # 기존에 수동으로 설정된 값들이 있을 수 있으므로, 
            # 파싱된 결과가 있을 때만 덮어쓰거나, 
            # 아니면 무조건 덮어쓸지 결정해야 함. 
            # 사용자 요청: "Desc 부분을 참조해서 설명 맞도록 모든 스킬의 Effects 관련 부분을 수정"
            # -> 무조건 덮어쓰기 (단, 파싱 결과가 비어있으면 기존 유지 고려)
            
            if new_effects:
                skill['effects'] = new_effects
                updated_count += 1
            
            # Animation 필드 보존 (이전 작업)
            if 'animation' not in skill:
                skill['animation'] = "Salute"

    with open(file_path, 'w', encoding='utf-8') as f:
        json.dump(data, f, indent=2, ensure_ascii=False)
        
    print(f"Updated {updated_count} skills.")

if __name__ == "__main__":
    process_skill_list()
