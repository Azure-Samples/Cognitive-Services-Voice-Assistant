namespace AudioPlayer{
    class AudioPlayerEntry{
        public:
            AudioPlayerEntry(unsigned char* pData, size_t pSize);
            uint m_size;
            unsigned char *m_data;
    };
}