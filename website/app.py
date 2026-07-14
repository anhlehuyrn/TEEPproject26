import streamlit as st
import pandas as pd
import json
import os
import plotly.express as px

# ==========================================
# 1. TỪ ĐIỂN ĐA NGÔN NGỮ (LOCALIZATION)
# ==========================================
LANGUAGES = {
    "en": "English",
    "zh-TW": "繁體中文 (Taiwan)",
    "ml": "മലയാളം (Kerala)",
    "vi": "Tiếng Việt"
}

CULTURES = {
    "vietnam": "�🇳 Vietnam",
    "kerala": "🇮🇳 Kerala",
    "taiwan": "🇹🇼 Taiwan"
}

# Sau này bạn có thể tách cục TRANSLATIONS này ra một file locales.json riêng để code gọn hơn
TRANSLATIONS = {
    "en": {
        "app_title": "🏛️ DongHo.AI: Interactive Cultural Exhibition",
        "menu": "Main Menu",
        "nav_gallery": "🖼️ Gallery",
        "nav_analytics": "📊 Data Analytics",
        "nav_comments": "💬 Guestbook",
        "lang_select": "🌐 Select Language",
        "culture_select": "🎭 Choose Culture Theme",
        "gallery_desc": "Explore and scan cultural artworks to uncover hidden layers.",
        "btn_scan": "👁️ Simulate user scanning (Test)",
        "success_scan": "1 interaction recorded!",
        "chart_title": "AR Scans per Artwork"
    },
    "zh-TW": {
        "app_title": "🏛️ DongHo.AI: 互動式文化展覽",
        "menu": "主選單",
        "nav_gallery": "🖼️ 展覽館",
        "nav_analytics": "📊 數據分析",
        "nav_comments": "💬 留言板",
        "lang_select": "🌐 選擇語言",
        "culture_select": "🎭 選擇文化主題",
        "gallery_desc": "探索並掃描文化作品，挖掘隱藏的意涵。",
        "btn_scan": "👁️ 模擬使用者掃描 (測試)",
        "success_scan": "已記錄 1 次互動！",
        "chart_title": "各畫作 AR 掃描次數"
    },
    "ml": {
        "app_title": "🏛️ DongHo.AI: ഇന്ററാക്ടീവ് സാംസ്കാരിക പ്രദർശനം",
        "menu": "പ്രധാന മെനു",
        "nav_gallery": "🖼️ ഗാലറി",
        "nav_analytics": "📊 ഡാറ്റ അനലിറ്റിക്സ്",
        "nav_comments": "💬 അഭിപ്രായങ്ങൾ",
        "lang_select": "🌐 ഭാഷ തിരഞ്ഞെടുക്കുക",
        "culture_select": "🎭 സാംസ്കാരിക തീം തിരഞ്ഞെടുക്കുക",
        "gallery_desc": "മറഞ്ഞിരിക്കുന്ന അർത്ഥങ്ങൾ കണ്ടെത്താൻ ചിത്രങ്ങൾ സ്കാൻ ചെയ്യുക.",
        "btn_scan": "👁️ സ്കാൻ ചെയ്യുന്നത് അനുകരിക്കുക (Test)",
        "success_scan": "1 ഇന്ററാക്ഷൻ രേഖപ്പെടുത്തി!",
        "chart_title": "ഓരോ ചിത്രത്തിന്റെയും AR സ്കാനുകൾ"
    },
    "vi": {
        "app_title": "🏛️ DongHo.AI: Triển lãm Văn hóa Tương tác",
        "menu": "Menu Chính",
        "nav_gallery": "🖼️ Phòng trưng bày",
        "nav_analytics": "📊 Phân tích dữ liệu",
        "nav_comments": "💬 Sổ lưu bút",
        "lang_select": "🌐 Chọn ngôn ngữ",
        "culture_select": "🎭 Chọn chủ đề văn hóa",
        "gallery_desc": "Khám phá và quét các tác phẩm để tìm hiểu về văn hóa.",
        "btn_scan": "👁️ Mô phỏng người dùng quét tranh (Test)",
        "success_scan": "Đã ghi nhận 1 lượt tương tác!",
        "chart_title": "Số lượt quét AR theo từng tác phẩm"
    }
}

# Hàm helper lấy text theo ngôn ngữ đang chọn
def _t(key):
    lang = st.session_state.lang
    # Trả về text dịch, nếu không có thì trả về key gốc
    return TRANSLATIONS.get(lang, TRANSLATIONS["vi"]).get(key, key)

# ==========================================
# 2. KHỞI TẠO SESSION STATE (BỘ NHỚ)
# ==========================================
st.set_page_config(page_title="DongHo.AI Web", layout="wide")

if "lang" not in st.session_state:
    st.session_state.lang = "en"
    
if "culture" not in st.session_state:
    st.session_state.culture = "vietnam"

if "current_page" not in st.session_state:
    st.session_state.current_page = "nav_gallery"

# Khởi tạo Database giả lập
DATA_FILE = "gallery_data.json"
if not os.path.exists(DATA_FILE):
    with open(DATA_FILE, "w") as f:
        json.dump({
            "interactions": {
                "Đám cưới chuột": 120, "Quan Họ": 85, "Lợn âm dương": 45,
                "Kathakali": 60, "Theyyam": 40,
                "Glove Puppetry": 70, "Lantern Festival": 90
            }, 
            "comments": []
        }, f)

with open(DATA_FILE, "r") as f:
    db = json.load(f)

# ==========================================
# 3. SIDEBAR (ĐIỀU HƯỚNG & NGÔN NGỮ)
# ==========================================
# Đổi ngôn ngữ
st.sidebar.subheader(_t("lang_select"))
lang_names = list(LANGUAGES.values())
current_lang_idx = list(LANGUAGES.keys()).index(st.session_state.lang)

selected_lang_name = st.sidebar.selectbox(
    "Language", 
    options=lang_names, 
    index=current_lang_idx,
    label_visibility="collapsed"
)
st.session_state.lang = list(LANGUAGES.keys())[lang_names.index(selected_lang_name)]

# Chọn chủ đề văn hóa
st.sidebar.markdown("---")
st.sidebar.subheader(_t("culture_select"))
culture_names = list(CULTURES.values())
current_culture_idx = list(CULTURES.keys()).index(st.session_state.culture)

selected_culture_name = st.sidebar.selectbox(
    "Culture",
    options=culture_names,
    index=current_culture_idx,
    label_visibility="collapsed"
)
st.session_state.culture = list(CULTURES.keys())[culture_names.index(selected_culture_name)]

st.sidebar.markdown("---")
st.sidebar.subheader(_t("menu"))

# Nút bấm điều hướng (Dùng key cố định để không bị reset khi đổi ngôn ngữ)
if st.sidebar.button(_t("nav_gallery"), use_container_width=True):
    st.session_state.current_page = "nav_gallery"
if st.sidebar.button(_t("nav_analytics"), use_container_width=True):
    st.session_state.current_page = "nav_analytics"
if st.sidebar.button(_t("nav_comments"), use_container_width=True):
    st.session_state.current_page = "nav_comments"

# ==========================================
# 4. HIỂN THỊ GIAO DIỆN THEO TRẠNG THÁI
# ==========================================
st.title(_t("app_title"))

# TRANG 1: GALLERY
if st.session_state.current_page == "nav_gallery":
    st.markdown(_t("gallery_desc"))
    
    # Dữ liệu mẫu cho từng văn hóa
    culture_data = {
        "vietnam": [
            {"name": "Đám cưới chuột", "image": "https://media-cdn-v2.laodong.vn/storage/newsportal/2020/1/20/779763/Dam-Cuoi-Chuot.jpg"},
            {"name": "Quan Họ", "image": "https://upload.wikimedia.org/wikipedia/commons/thumb/c/cf/Tranh_Quan_ho.jpg/800px-Tranh_Quan_ho.jpg"}
        ],
        "kerala": [
            {"name": "Kathakali", "image": "https://upload.wikimedia.org/wikipedia/commons/2/2c/Kathakali_-Play_with_Kaurava.jpg"},
            {"name": "Theyyam", "image": "https://lumiereholidays.com/storage/pageContent/176311869869170e6a7a793.jpeg"}
        ],
        "taiwan": [
            {"name": "Glove Puppetry", "image": "https://upload.wikimedia.org/wikipedia/commons/thumb/8/87/Pili_Puppet_Show.jpg/800px-Pili_Puppet_Show.jpg"},
            {"name": "Lantern Festival", "image": "https://upload.wikimedia.org/wikipedia/commons/thumb/c/c5/Pingxi_Sky_Lantern_Festival.jpg/800px-Pingxi_Sky_Lantern_Festival.jpg"}
        ]
    }

    current_items = culture_data.get(st.session_state.culture, culture_data["vietnam"])
    
    col1, col2 = st.columns(2)
    with col1:
        item = current_items[0]
        st.image(item["image"], caption=item["name"])
        if st.button(_t("btn_scan"), key=f"scan_{item['name']}"):
            db["interactions"][item["name"]] = db["interactions"].get(item["name"], 0) + 1
            with open(DATA_FILE, "w") as f: json.dump(db, f)
            st.success(_t("success_scan"))
            
    with col2:
        item = current_items[1]
        st.image(item["image"], caption=item["name"])
        if st.button(_t("btn_scan"), key=f"scan_{item['name']}"):
            db["interactions"][item["name"]] = db["interactions"].get(item["name"], 0) + 1
            with open(DATA_FILE, "w") as f: json.dump(db, f)
            st.success(_t("success_scan"))

# TRANG 2: ANALYTICS
elif st.session_state.current_page == "nav_analytics":
    df = pd.DataFrame(list(db["interactions"].items()), columns=["Artwork", "Scans"])
    fig = px.bar(df, x="Artwork", y="Scans", color="Artwork", title=_t("chart_title"))
    st.plotly_chart(fig, use_container_width=True)

# TRANG 3: COMMENTS
elif st.session_state.current_page == "nav_comments":
    for c in reversed(db["comments"]):
        st.info(f"**{c['name']}** (về *{c['artwork']}*):\n\n{c['comment']}")