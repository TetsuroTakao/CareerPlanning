import { useState, useRef } from 'react';

export const useAudioRecorder = () => {
  const [isRecording, setIsRecording] = useState(false);
  const mediaRecorderRef = useRef<MediaRecorder | null>(null);
  const audioChunksRef = useRef<Blob[]>([]);

  const startRecording = async () => {
    audioChunksRef.current = [];
    const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
    
    // wav形式を指定（ブラウザの互換性に注意。対応していない場合は audio/webm になります）
    const options = { mimeType: 'audio/webm;codecs=opus' }; 
    const mediaRecorder = new MediaRecorder(stream, options);
    
    mediaRecorderRef.current = mediaRecorder;

    // 5秒ごとにデータが生成された時の処理
    mediaRecorder.ondataavailable = async (event) => {
      if (event.data && event.data.size > 0) {
        audioChunksRef.current.push(event.data);
        
        // 【ベストプラクティス】
        // ここで5秒ごとに分割データをAPIに送信（アップロード）する処理を呼ぶと、
        // 途中で切断されてもそこまでのデータが保護されます。
        await uploadChunkToBackend(event.data);
      }
    };

    // 5000ms (5秒) ごとに ondataavailable を発火させる
    mediaRecorder.start(5000); 
    setIsRecording(true);
  };

  const stopRecording = () => {
    if (mediaRecorderRef.current && isRecording) {
      mediaRecorderRef.current.stop();
      // すべてのマイク入力を停止してブラウザの録音マークを消す
      mediaRecorderRef.current.stream.getTracks().forEach(track => track.stop());
      setIsRecording(false);
    }
  };

  const uploadChunkToBackend = async (chunk: Blob) => {
    // TODO: ここで、一時URLやストリーム用エンドポイントにチャンクをPUT/POSTする
    console.log("Uploading chunk of size:", chunk.size);
  };

  return { isRecording, startRecording, stopRecording };
};