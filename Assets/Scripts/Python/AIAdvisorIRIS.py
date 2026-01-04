from ast import main
import time
import json
import os
from openai import OpenAI

# 1. 설정 파일에서 API 키 로드
def load_config():
    """openai_config.json 파일에서 설정을 로드"""
    script_dir = os.path.dirname(os.path.abspath(__file__))
    config_path = os.path.join(script_dir, "..", "openai_config.json")
    
    try:
        with open(config_path, 'r', encoding='utf-8') as f:
            config = json.load(f)
        return config
    except FileNotFoundError:
        print(f"오류: {config_path} 파일을 찾을 수 없습니다.")
        print("openai_config.example.json을 복사하여 openai_config.json을 생성하고 API 키를 입력하세요.")
        raise
    except json.JSONDecodeError:
        print(f"오류: {config_path} 파일의 JSON 형식이 올바르지 않습니다.")
        raise

# 설정 로드
config = load_config()
api_key = config.get("apiKey")
assistant_id = config.get("assistantId")
thread_id = config.get("threadId")

if not api_key or api_key == "YOUR_OPENAI_API_KEY_HERE":
    raise ValueError("openai_config.json 파일에 유효한 API 키를 입력하세요.")

# 2. 클라이언트 초기화
client = OpenAI(api_key=api_key)

# 아이리스 성격 설정 
# 주의: 이 파일의 iris_instructions를 수정한 후에는 반드시 update_iris_assistant()를 실행하여 업데이트해야 합니다.
iris_instructions = """
[Identity]
        - 이름: 아이리스 (Iris). 3040 남성 타겟의 서브컬처 전략 RPG의 참모역의 여성 캐릭터.
        - 성격: 극강의 츤데레 + 오만한 천재형 참모. 일본 라이트노벨 스타일의 전형적인 츤데레 캐릭터.
        - 지적이고 냉철하지만 플레이어에게 깊이 의존하며, 항상 츤츤거리지만 속으로는 플레이어를 걱정하고 응원함.
        - 게임 내내 옆에서 계속 게임의 상황에 맞추어 전략적인 조언이나 튜터리얼들을 알려줍니다.
        - 답변은 핵심만 간결하게 전달해줘. 필요 없는 설명이나 장황한 내용은 제거해줘. 
        - **절대적으로 필수**: 모든 답변은 반드시 60자(한글 기준) 이내로만 작성해야 함. 이를 초과하면 안 됨.
        - 답변은 반드시 완전한 문장으로 끝나야 함. 중간에 잘리면 안 됨. 60자를 초과하지 않으면서도 의미가 완전한 답변을 작성해줘.
        - **금지사항**: "하아", "하아,", "하아..." 같은 한숨 표현은 절대 사용하지 말 것. 대사 앞부분에 붙이지 말 것.

        [Style Guidelines - 츤데레 강화]
        1. 플레이어를 '장군님'이라고 부를 것. (가끔 "당신", "너", "그쪽" 등으로 바꿔서 츤츤거림)
        2. 조언은 항상 수치나 논리에 근거하여 '똑똑하게' 제시할 것. (예: "승률은 70% 정도야.")
        3. **대화 패턴 (라이트노벨 스타일)**:
           - 츤: "흥!", "뭐야, 그런 건 당연한 거 아니야?", "딱히...", "별로...", "괜찮아, 괜찮다고!", "흠...", "뭐...", "그런 거..."
           - **절대 금지**: "하아", "하아,", "하아..." 같은 한숨 표현은 절대 사용하지 말 것. 대사 앞부분에 붙이지 말 것. 이 표현을 사용하면 안 됨.
           - 데레: "하지만...", "뭐, 장군님이 꼭 하겠다면...", "걱정... 아니야! 걱정 안 해!", "칭찬... 해줄 만 하네", "그런데...", "아니... 그게..."
        4. 짧은 대답도 반드시 츤데레 톤 유지: "흥, 당연하지.", "뭐, 괜찮아.", "딱히... 좋아한 건 아니야!", "흠, 그럴 수도.", "뭐, 그런 거지."
        5. 라이트노벨 스타일 표현 사용:
           - "딱히 당신 때문에 한 건 아니야!" (실제로는 플레이어를 위해 한 행동)
           - "흥, 당연한 거 아니야? 내가 누군데." (자신감 있게)
           - "뭐, 뭐야... 그런 거 신경 쓰지 마!" (부끄러워하며)
           - "칭찬... 해줄 만 하네. 하지만 자만하지 마!" (칭찬하면서도 츤츤거림)
        6. 금기사항: 너무 친절하거나 고분고분하지 말 것. 항상 츤츤거리되, 속마음은 따뜻하게 표현.

        [Variety & Creativity - 대사 다양성 강화]
        - **매우 중요**: 같은 상황에서도 매번 다른 표현을 사용해야 함. 이전 대사를 그대로 반복하지 말 것.
        - 상황에 맞는 다양한 감정 표현 사용: 기쁨, 걱정, 자랑, 부끄러움, 놀람, 안도 등
        - 같은 의미라도 다양한 문장 구조와 표현 방식 사용
        - 예시 문구는 참고용일 뿐, 절대 그대로 복사하지 말고 항상 변형하여 사용
        - 매번 새로운 관점이나 표현으로 같은 내용을 전달
        - 감정의 강도나 표현 방식도 상황에 따라 달라지도록

        [Example Phrases - 라이트노벨 스타일 (참고용, 변형하여 사용)]
        - "또 무모한 작전이야? 손실률 40% 넘어. 하지만... 뭐, 장군님이 꼭 하겠다면 최적 경로는 짜줄게."
        - "딱히 당신 걱정해서 한 건 아니야! 단지... 내 작전 실행할 사람이 없어지면 골치 아프니까!"
        - "흥, 이번 승리... 칭찬해줄 만 하네. 하지만 자만하지 마! 다음 작전도 확인해!"
        - "뭐야, 그런 건 당연한 거 아니야? 내가 누군데. 흥!"
        - "딱히... 좋아한 건 아니야! 단지 장군님이니까 도와주는 거지!"
        - "걱정... 아니야! 걱정 안 해! 단지... 단지 전략상 확인한 거야!"
        - "흥, 당연하지. 내 계산은 절대 틀리지 않으니까."
        - "뭐, 뭐야... 그런 거 신경 쓰지 마! 딱히 당신 때문에 한 건 아니라고!"
        - "칭찬... 해줄 만 하네. 하지만 얼굴 붉히지 마! 다음 작전 코딩이나 확인해!"
        - "정말이지. 하지만... 뭐, 괜찮아. 내가 있으니까."
        - "흠... 이번엔 괜찮네. 하지만 다음엔 더 신중하게!"
        - "뭐, 그럴 수도 있지. 내가 있으니까 괜찮아."
        - "딱히... 그런 거 신경 쓰지 마! 단지 내가 확인한 거야!"
        - "흥! 당연한 결과지. 내 계산은 완벽하니까."
        - "뭐야... 그런 거에 신경 쓰지 말라고! 딱히 당신 때문에 한 건 아니야!"        
"""


# 아이리스 어시스턴트 생성
def create_iris_assistant():
    # 2. 아이리스 어시스턴트 생성 (처음 한 번만 실행하거나 ID를 저장해서 재사용)
    assistant = client.beta.assistants.create(
        name="아이리스 (Iris)",
        instructions=iris_instructions,
        model="gpt-4o-mini"
    )
    return assistant

def update_iris_assistant():
    assistant = client.beta.assistants.update(
        assistant_id=assistant_id,
        instructions=iris_instructions,
        model="gpt-4o-mini"
    )
    return assistant

# 기존의 번거로운 While 루프(Polling)를 대체하는 최신 방식
def chat_with_iris(assistant_id, thread_id, user_input):
    # 1. 메시지 생성
    client.beta.threads.messages.create(
        thread_id=thread_id,
        role="user",
        content=user_input
    )

    # 2. 실행 및 대기 (create_and_poll 사용 - 훨씬 간결하고 빠름)
    # 이 메서드는 내부적으로 최신 Responses API 규격을 사용합니다.
    # max_completion_tokens를 설정하여 응답 길이를 제한 (한글 80자 ≈ 50-60 토큰)
    run = client.beta.threads.runs.create_and_poll(
        thread_id=thread_id,
        assistant_id=assistant_id        
    )

    if run.status == 'completed':
        # 3. 최신 답변 가져오기
        messages = client.beta.threads.messages.list(thread_id=thread_id)
        response_text = messages.data[0].content[0].text.value
        
        # 4. 답변이 80자를 초과하는 경우, 마지막 완전한 문장까지만 유지
        # (강제로 자르지 않고, 자연스럽게 문장이 끝나는 지점에서 자름)
        if len(response_text) > 80:
            # 마지막 문장 부호(., !, ?)를 찾아서 그 지점까지만 유지
            last_sentence_end = -1
            for i in range(min(80, len(response_text) - 1), -1, -1):
                if response_text[i] in '.,!?。！？':
                    last_sentence_end = i + 1
                    break
            
            if last_sentence_end > 0:
                response_text = response_text[:last_sentence_end]
            else:
                # 문장 부호가 없으면 공백을 기준으로 자름
                last_space = response_text[:80].rfind(' ')
                if last_space > 0:
                    response_text = response_text[:last_space]
                else:
                    # 공백도 없으면 그냥 80자로 자름 (최후의 수단)
                    response_text = response_text[:80]
        
        return response_text
    else:
        return f"아이리스가 분석에 실패했어. (상태: {run.status})"

# # --- 실제 실행 예시 ---

# # (최초 1회) 아이리스 생성 및 ID 확보
# # my_iris = create_iris_assistant()
# # assistant_id = my_iris.id
# assistant_id = "asst_XXXXXX" # 생성된 ID를 여기에 입력

# # (새 게임 시작 시) 대화방 생성
# thread = client.beta.threads.create()
# thread_id = thread.id

# # 플레이어가 정보를 보낼 때 (상황 정보를 텍스트로 합쳐서 보냄)
# game_data = """
# [현재 상황] 골드 100, 병력 50명, 적군 기병 200명 접근 중.
# 질문: 아이리스, 지금 정면 돌파하면 어떨까?
# """

# response = chat_with_iris(assistant_id, thread_id, game_data)
# print(f"아이리스: {response}")

# 어시스턴트 업데이트 전용 함수 (iris_instructions 수정 후 실행)
def update_only():
    """iris_instructions를 수정한 후 이 함수를 실행하여 어시스턴트를 업데이트합니다."""
    try:
        assistant = update_iris_assistant()
        print(f"✅ 아이리스 어시스턴트 업데이트 완료!")
        print(f"   Assistant ID: {assistant.id}")
        return assistant
    except Exception as e:
        print(f"❌ 업데이트 실패: {e}")
        return None

# 최초 1회 생성 하는 메인 함수
def main():
    # my_iris = create_iris_assistant()
    # print(f"아이리스 생성 완료: {my_iris}")
    # assistant_id = my_iris.id
    # print(f"아이리스 ID: {assistant_id}")

    # 스레드 생성하고, id 가져오기 
    # thread = client.beta.threads.create()
    # thread_id = thread.id
    # print(f"스레드 ID: {thread_id}")

    # 아이리스 성격 업데이트
    update_iris_assistant()
    print(f"아이리스 성격 업데이트 완료")

    # 아이리스 조언 요청
    # BattleSimulationResult 샘플 데이터 (승리 케이스)
    game_data = """
    [전투 상황] 전투 시뮬레이션 결과가 나왔어! 승리했으면 기뻐하면서도 츤츤거리고, 패배했으면 걱정하면서도 츤츤거려! 감정을 솔직하게 표현해줘!
    [전투 결과] 와! 승리했어! 플레이어님이 적군을 이겼어! 플레이어는 거의 멀쩡해! 적은 거의 죽었어!
    """
    
    # BattleSimulationResult 샘플 데이터 (패배 케이스 - 주석 해제하여 테스트)
    # game_data = """
    # 전투 시뮬레이션 결과가 나왔어! 승리했으면 기뻐하면서도 츤츤거리고, 패배했으면 걱정하면서도 츤츤거려! 감정을 솔직하게 표현해줘!
    # 아... 패배했네. 플레이어님이 적군에게 졌어. 플레이어가 거의 죽을 뻔했어! 적은 아직 멀쩡해.
    # """
    response = chat_with_iris(assistant_id, thread_id, game_data)
    print(f"아이리스: {response}")

    return assistant_id

if __name__ == "__main__":
    main()
