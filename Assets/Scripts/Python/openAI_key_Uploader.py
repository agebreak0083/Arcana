"""
OpenAI Config 업로더

사용 전 필수 설치:
    pip install requests

사용 방법:
    python Assets/Scripts/Python/openAI_key_Uploader.py
"""

import json
import requests
import os
import sys
from pathlib import Path

# 서버 설정
BASE_URL = "https://arcana.koreacentral.cloudapp.azure.com/api"
UPLOAD_ENDPOINT = "/data"

def load_config_file(config_path):
    """
    openai_config.json 파일을 로드합니다.
    
    Args:
        config_path: JSON 파일 경로
        
    Returns:
        dict: JSON 파일 내용
    """
    try:
        with open(config_path, 'r', encoding='utf-8') as f:
            config_data = json.load(f)
        print(f"✓ 설정 파일 로드 성공: {config_path}")
        return config_data
    except FileNotFoundError:
        print(f"✗ 오류: 파일을 찾을 수 없습니다: {config_path}")
        return None
    except json.JSONDecodeError as e:
        print(f"✗ 오류: JSON 파싱 실패: {e}")
        return None
    except Exception as e:
        print(f"✗ 오류: 파일 로드 실패: {e}")
        return None

def upload_to_server(config_data):
    """
    openai_config.json 데이터를 JSON 서버에 업로드합니다.
    
    Args:
        config_data: 업로드할 JSON 데이터
        
    Returns:
        bool: 업로드 성공 여부
    """
    url = f"{BASE_URL}{UPLOAD_ENDPOINT}"
    
    # 서버 요청 형식에 맞게 데이터 구성
    request_body = {
        "id": "openai_config",
        "content": config_data
    }
    
    try:
        print(f"📤 서버에 업로드 중...")
        print(f"   URL: {url}")
        
        response = requests.post(
            url,
            json=request_body,
            headers={"Content-Type": "application/json"},
            timeout=30
        )
        
        if response.status_code == 200:
            print(f"✓ 업로드 성공!")
            print(f"   응답: {response.text[:200]}...")
            return True
        else:
            print(f"✗ 업로드 실패: HTTP {response.status_code}")
            print(f"   응답: {response.text}")
            return False
            
    except requests.exceptions.Timeout:
        print(f"✗ 오류: 요청 시간 초과 (30초)")
        return False
    except requests.exceptions.ConnectionError:
        print(f"✗ 오류: 서버 연결 실패")
        return False
    except requests.exceptions.RequestException as e:
        print(f"✗ 오류: 요청 실패: {e}")
        return False
    except Exception as e:
        print(f"✗ 오류: 예상치 못한 오류: {e}")
        return False

def main():
    """
    메인 함수: openai_config.json을 로드하고 서버에 업로드합니다.
    """
    # 스크립트 파일의 위치를 기준으로 상대 경로 계산
    script_dir = Path(__file__).parent
    project_root = script_dir.parent.parent.parent  # Scripts/Python -> Scripts -> Assets -> Project Root
    config_path = project_root / "Assets" / "Resources" / "openai_config.json"
    
    # 절대 경로로 변환
    config_path = config_path.resolve()
    
    print("=" * 60)
    print("OpenAI Config 업로더")
    print("=" * 60)
    print(f"설정 파일 경로: {config_path}")
    print()
    
    # 파일 존재 확인
    if not config_path.exists():
        print(f"✗ 오류: 파일이 존재하지 않습니다: {config_path}")
        sys.exit(1)
    
    # 설정 파일 로드
    config_data = load_config_file(config_path)
    if config_data is None:
        sys.exit(1)
    
    print()
    
    # 서버에 업로드
    success = upload_to_server(config_data)
    
    print()
    if success:
        print("=" * 60)
        print("✓ 모든 작업이 성공적으로 완료되었습니다!")
        print("=" * 60)
        sys.exit(0)
    else:
        print("=" * 60)
        print("✗ 업로드 실패")
        print("=" * 60)
        sys.exit(1)

if __name__ == "__main__":
    main()
